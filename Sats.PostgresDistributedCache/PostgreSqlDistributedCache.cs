using Npgsql;
using Sats.PostgresDistributedCache;

namespace Sats.PostgreSqlDistributedCache
{
    public class PostgreSqlDistributedCache : IPostgreSqlDistributedCache
    {
        private readonly string _connectionString;

        public PostgreSqlDistributedCache(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(token);

            const string query = "SELECT value FROM public.\"Cache\" WHERE key = @key AND (expiration IS NULL OR expiration > @current_time)";
            await using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("key", key);
            cmd.Parameters.AddWithValue("current_time", DateTime.UtcNow);

            var result = await cmd.ExecuteScalarAsync(token);
            return result is not DBNull ? (byte[])result : null;
        }

        public async Task SetAsync(string key, byte[] value, TimeSpan? expiration = null, CancellationToken token = default)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(token);

            const string query = @"
            INSERT INTO public.""Cache"" (key, value, expiration)
            VALUES (@key, @value, @expiration)
            ON CONFLICT (key) 
            DO UPDATE SET value = @value, expiration = @expiration";

            await using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("key", key);
            cmd.Parameters.AddWithValue("value", value);
            cmd.Parameters.AddWithValue("expiration", expiration.HasValue ? (object)DateTime.UtcNow.Add(expiration.Value) : DBNull.Value);

            await cmd.ExecuteNonQueryAsync(token);
        }

        public async Task RemoveAsync(string key, CancellationToken token = default)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(token);

            const string query = "DELETE FROM public.\"Cache\" WHERE key = @key";
            await using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("key", key);

            await cmd.ExecuteNonQueryAsync(token);
        }

        public async Task RefreshAsync(string key, CancellationToken token = default)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(token);

            const string query = "UPDATE public.\"Cache\" SET expiration = @expiration WHERE key = @key";
            await using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("key", key);
            cmd.Parameters.AddWithValue("expiration", DateTime.UtcNow.AddMinutes(5));

            await cmd.ExecuteNonQueryAsync(token);
        }
    }
}
