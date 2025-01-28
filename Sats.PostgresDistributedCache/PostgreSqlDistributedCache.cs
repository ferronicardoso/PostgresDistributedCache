
using Microsoft.Extensions.Caching.Distributed;
using Npgsql;

namespace Sats.PostgresDistributedCache
{
    public class PostgreSqlDistributedCache : IDistributedCache
    {
        private readonly string _connectionString;

        public PostgreSqlDistributedCache(string connectionString)
        {
            _connectionString = connectionString;
        }

        public byte[]? Get(string key)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            var query = "SELECT data FROM cache WHERE key = @key AND expiration > @current_time";
            using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("key", key);
            cmd.Parameters.AddWithValue("current_time", DateTime.UtcNow);

            var result = cmd.ExecuteScalar();
            return result != DBNull.Value ? (byte[])result : null;
        }

        public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(token);

            var query = "SELECT data FROM cache WHERE key = @key AND expiration > @current_time";
            using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("key", key);
            cmd.Parameters.AddWithValue("current_time", DateTime.UtcNow);

            var result = await cmd.ExecuteScalarAsync(token);
            return result != DBNull.Value ? (byte[])result : null;
        }

        public void Refresh(string key)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            var query = "UPDATE cache SET expiration = @expiration WHERE key = @key";
            using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("key", key);
            cmd.Parameters.AddWithValue("expiration", DateTime.UtcNow.AddMinutes(5)); // Extend the expiration by 5 minutes

            cmd.ExecuteNonQuery();
        }

        public async Task RefreshAsync(string key, CancellationToken token = default)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(token);

            var query = "UPDATE cache SET expiration = @expiration WHERE key = @key";
            using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("key", key);
            cmd.Parameters.AddWithValue("expiration", DateTime.UtcNow.AddMinutes(5));

            await cmd.ExecuteNonQueryAsync(token);
        }

        public void Remove(string key)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            var query = "DELETE FROM cache WHERE key = @key";
            using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("key", key);

            cmd.ExecuteNonQuery();
        }

        public async Task RemoveAsync(string key, CancellationToken token = default)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(token);

            var query = "DELETE FROM cache WHERE key = @key";
            using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("key", key);

            await cmd.ExecuteNonQueryAsync(token);
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            var query = "INSERT INTO cache (key, data, expiration) VALUES (@key, @data, @expiration) " +
                        "ON CONFLICT (key) DO UPDATE SET data = @data, expiration = @expiration";
            using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("key", key);
            cmd.Parameters.AddWithValue("data", value);
            cmd.Parameters.AddWithValue("expiration", options.AbsoluteExpiration?.UtcDateTime ?? DateTime.UtcNow.AddMinutes(5));

            cmd.ExecuteNonQuery();
        }

        public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(token);

            var query = "INSERT INTO cache (key, data, expiration) VALUES (@key, @data, @expiration) " +
                        "ON CONFLICT (key) DO UPDATE SET data = @data, expiration = @expiration";
            using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("key", key);
            cmd.Parameters.AddWithValue("data", value);
            cmd.Parameters.AddWithValue("expiration", options.AbsoluteExpiration?.UtcDateTime ?? DateTime.UtcNow.AddMinutes(5));

            await cmd.ExecuteNonQueryAsync(token);
        }
    }
}
