using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Npgsql;
using Sats.PostgresDistributedCache;

namespace Sats.PostgreSqlDistributedCache
{
    public class PostgreSqlDistributedCache : IPostgreSqlDistributedCache
    {
        private readonly PostgresDistributedCacheOptions _options;

        public PostgreSqlDistributedCache(string connectionString)
            : this(new PostgresDistributedCacheOptions { ConnectionString = connectionString }) { }

        public PostgreSqlDistributedCache(IOptions<PostgresDistributedCacheOptions> options)
        {
            _options = options.Value;
        }

        private async Task<NpgsqlConnection> CreateOpenConnectionAsync(CancellationToken token)
        {

            var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(token);

            string checkTableQuery = $@"
                SELECT EXISTS (
                    SELECT FROM information_schema.tables 
                    WHERE table_schema = @schema_name
                    AND table_name = @table_name
                )";

            await using var checkCmd = new NpgsqlCommand(checkTableQuery, connection);
            checkCmd.Parameters.AddWithValue("schema_name", _options.SchemaName);
            checkCmd.Parameters.AddWithValue("table_name", _options.TableName);

            bool tableExists = (bool)(await checkCmd.ExecuteScalarAsync(token) ?? false);


            if (!tableExists)
            {
                // Generate unique constraint names using a prefix to avoid conflicts
                string pkConstraintName = $"pk_dist_cache_{Guid.NewGuid():N}".Substring(0, 30);
                string uqConstraintName = $"uq_dist_cache_{Guid.NewGuid():N}".Substring(0, 30);

                string createTableQuery = $@"
                    CREATE TABLE IF NOT EXISTS {_options.SchemaName}.""{_options.TableName}"" (
                        key character varying(255) NOT NULL,
                        value bytea NOT NULL,
                        expiration timestamp with time zone,
                        CONSTRAINT ""{pkConstraintName}"" PRIMARY KEY (key),
                        CONSTRAINT ""{uqConstraintName}"" UNIQUE (key)
                    )";

                try
                {
                    await using var createCmd = new NpgsqlCommand(createTableQuery, connection);
                    await createCmd.ExecuteNonQueryAsync(token);
                }
                catch (Npgsql.PostgresException ex) when (ex.SqlState == "23505") // Duplicate key violation
                {
                    // Table might have been created by another concurrent request
                    // Verify the table exists now before proceeding
                    await using var verifyCmd = new NpgsqlCommand(checkTableQuery, connection);
                    verifyCmd.Parameters.AddWithValue("schema_name", _options.SchemaName);
                    verifyCmd.Parameters.AddWithValue("table_name", _options.TableName);

                    bool tableExistsNow = (bool)(await verifyCmd.ExecuteScalarAsync(token) ?? false);
                    if (!tableExistsNow)
                    {
                        // If the table still doesn't exist, re-throw the exception
                        throw;
                    }
                }
            }
            return connection;
        }

        public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        {
            string query =
                $@"SELECT value FROM {_options.SchemaName}.""{_options.TableName}"" WHERE key = @key AND (expiration IS NULL OR expiration > @current_time)";

            await using var connection = await CreateOpenConnectionAsync(token);
            await using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("key", key);
            cmd.Parameters.AddWithValue("current_time", DateTime.UtcNow);

            var result = await cmd.ExecuteScalarAsync(token);
            return result is DBNull ? null : result as byte[];
        }

        public async Task SetAsync(string key, byte[] value, TimeSpan? expiration = null, CancellationToken token = default)
        {
            string query = $@"
                    INSERT INTO {_options.SchemaName}.""{_options.TableName}"" (key, value, expiration)
                    VALUES (@key, @value, @expiration)
                    ON CONFLICT (key)
                    DO UPDATE SET value = @value, expiration = @expiration";

            await using var connection = await CreateOpenConnectionAsync(token);
            await using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("key", key);
            cmd.Parameters.AddWithValue("value", value);
            cmd.Parameters.AddWithValue("expiration", expiration.HasValue ? (object)DateTime.UtcNow.Add(expiration.Value) : DBNull.Value);

            await cmd.ExecuteNonQueryAsync(token);
        }

        public async Task RemoveAsync(string key, CancellationToken token = default)
        {
            string query = $@"DELETE FROM {_options.SchemaName}.""{_options.TableName}"" WHERE key = @key";

            await using var connection = await CreateOpenConnectionAsync(token);
            await using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("key", key);

            await cmd.ExecuteNonQueryAsync(token);
        }

        public async Task RefreshAsync(string key, CancellationToken token = default)
        {
            string query = $@"UPDATE {_options.SchemaName}.""{_options.TableName}"" SET expiration = @expiration WHERE key = @key";

            await using var connection = await CreateOpenConnectionAsync(token);
            await using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("key", key);
            cmd.Parameters.AddWithValue("expiration", DateTime.UtcNow.AddMinutes(5));

            await cmd.ExecuteNonQueryAsync(token);
        }

        public async Task<string?> GetStringAsync(string key, CancellationToken token = default)
        {
            var result = await GetAsync(key, token);
            return result != null ? Encoding.UTF8.GetString(result) : null;
        }

        public async Task SetStringAsync(string key, string value, TimeSpan? expiration = null, CancellationToken token = default)
        {
            var encodedValue = Encoding.UTF8.GetBytes(value);
            await SetAsync(key, encodedValue, expiration, token);
        }

        public byte[]? Get(string key) => GetAsync(key).GetAwaiter().GetResult();

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) =>
            SetAsync(key, value, options).GetAwaiter().GetResult();

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options,
            CancellationToken token = new CancellationToken()) =>
            SetAsync(key, value, options.AbsoluteExpirationRelativeToNow, token);

        public void Refresh(string key) => RefreshAsync(key).GetAwaiter().GetResult();

        public void Remove(string key) => RemoveAsync(key).GetAwaiter().GetResult();
    }
}
