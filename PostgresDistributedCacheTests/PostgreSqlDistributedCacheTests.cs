using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Npgsql;
using Sats.PostgresDistributedCache;
using Sats.PostgreSqlDistributedCache;

namespace PostgresDistributedCacheTests;

public class PostgreSqlDistributedCacheTests
{
    private PostgreSqlDistributedCache CreateCache()
    {
        var options = new PostgresDistributedCacheOptions
        {
            ConnectionString = "Server=127.0.0.1;Port=5432;Database=HIS;User Id=postgres;Password=satsvelke@7;",
            // You can use any valid PostgreSQL schema name. Default is 'public', but you can create and use custom schemas
            SchemaName = "public",
            // You can use any table name like 'Cache' or 'cache' according to your preferences
            TableName = "DCache"
        };
        return new PostgreSqlDistributedCache(Options.Create(options));
    }

    [Fact]
    public async Task SetAsync_Should_InsertOrUpdate_Cache()
    {
        var cache = CreateCache();
        string key = "testKey";
        byte[] value = Encoding.UTF8.GetBytes("testValue");

        await cache.SetAsync(key, value, TimeSpan.FromMinutes(10));

        var result = await cache.GetAsync(key);
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetAsync_Should_Return_StoredValue()
    {
        var cache = CreateCache();
        string key = "existingKey";
        byte[] expectedValue = Encoding.UTF8.GetBytes("existingValue");

        await cache.SetAsync(key, expectedValue);

        var result = await cache.GetAsync(key);

        Assert.NotNull(result);
        Assert.Equal(expectedValue, result);
    }

    [Fact]
    public async Task GetAsync_Should_Return_Null_If_Key_Not_Exists()
    {
        var cache = CreateCache();
        string key = "nonExistingKey";

        var result = await cache.GetAsync(key);

        Assert.Null(result);
    }

    [Fact]
    public async Task RemoveAsync_Should_Delete_Key()
    {
        var cache = CreateCache();
        string key = "testKey";
        byte[] value = Encoding.UTF8.GetBytes("testValue");

        await cache.SetAsync(key, value);

        await cache.RemoveAsync(key);
        var result = await cache.GetAsync(key);

        Assert.Null(result);
    }

    [Fact]
    public async Task RefreshAsync_Should_Extend_Expiration()
    {
        var cache = CreateCache();
        string key = "testKey";
        byte[] value = Encoding.UTF8.GetBytes("testValue");

        await cache.SetAsync(key, value, TimeSpan.FromMinutes(1));

        await cache.RefreshAsync(key);
        var result = await cache.GetAsync(key);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetStringAsync_Should_Return_String()
    {
        var cache = CreateCache();
        string key = "stringKey";
        string expectedValue = "Hello, World!";
        await cache.SetStringAsync(key, expectedValue);

        var result = await cache.GetStringAsync(key);

        Assert.Equal(expectedValue, result);
    }

    [Fact]
    public async Task GetStringAsync_Should_Return_Null_For_NonExistingKey()
    {
        var cache = CreateCache();
        string key = "nonExistingStringKey";

        var result = await cache.GetStringAsync(key);

        Assert.Null(result);
    }

    [Fact]
    public async Task SetStringAsync_Should_Store_String_As_Bytes()
    {
        var cache = CreateCache();
        string key = "testStringKey";
        string value = "Test String";

        await cache.SetStringAsync(key, value);
        var result = await cache.GetStringAsync(key);

        Assert.NotNull(result);

        Assert.Equal(value, result);
    }

    [Fact]
    public void Get_Should_Return_StoredValue_Synchronously()
    {
        var cache = CreateCache();
        string key = "syncGetKey";
        byte[] expectedValue = Encoding.UTF8.GetBytes("syncGetValue");

        cache.Set(key, expectedValue, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        });

        var result = cache.Get(key);

        Assert.NotNull(result);
        Assert.Equal(expectedValue, result);
    }

    [Fact]
    public void Get_Should_Return_Null_For_NonExistingKey_Synchronously()
    {
        var cache = CreateCache();
        string key = "nonExistingSyncKey";

        var result = cache.Get(key);

        Assert.Null(result);
    }

    [Fact]
    public void Set_With_Options_Should_Store_Value_Synchronously()
    {
        var cache = CreateCache();
        string key = "syncSetKey";
        byte[] value = Encoding.UTF8.GetBytes("syncSetValue");

        cache.Set(key, value, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        });

        var result = cache.Get(key);
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public void Remove_Should_Delete_Key_Synchronously()
    {
        var cache = CreateCache();
        string key = "syncRemoveKey";
        byte[] value = Encoding.UTF8.GetBytes("syncRemoveValue");

        cache.Set(key, value, new DistributedCacheEntryOptions());

        cache.Remove(key);
        var result = cache.Get(key);

        Assert.Null(result);
    }

    [Fact]
    public void Refresh_Should_Extend_Expiration_Synchronously()
    {
        var cache = CreateCache();
        string key = "syncRefreshKey";
        byte[] value = Encoding.UTF8.GetBytes("syncRefreshValue");

        cache.Set(key, value, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
        });

        cache.Refresh(key);
        var result = cache.Get(key);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task SetAsync_With_Options_Should_Store_Value()
    {
        var cache = CreateCache();
        string key = "asyncSetOptionsKey";
        byte[] value = Encoding.UTF8.GetBytes("asyncSetOptionsValue");

        await cache.SetAsync(key, value, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        });

        var result = await cache.GetAsync(key);
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task SetAsync_With_SlidingExpiration_Should_Store_Value()
    {
        var cache = CreateCache();
        string key = "slidingExpirationKey";
        byte[] value = Encoding.UTF8.GetBytes("slidingExpirationValue");

        await cache.SetAsync(key, value, new DistributedCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(5)
        });

        var result = await cache.GetAsync(key);
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task Cache_Should_Be_Usable_As_IDistributedCache()
    {
        // This test demonstrates that the cache can be used through IDistributedCache interface
        IDistributedCache cache = CreateCache();
        string key = "interfaceKey";
        byte[] value = Encoding.UTF8.GetBytes("interfaceValue");

        await cache.SetAsync(key, value, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        });

        var result = await cache.GetAsync(key);

        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task AutoTableCreation_Should_CreateTableIfNotExists()
    {
        // Arrange: Prepare the options and drop the table before testing.
        var options = new PostgresDistributedCacheOptions
        {
            ConnectionString = "Server=127.0.0.1;Port=5432;Database=HIS;User Id=postgres;Password=satsvelke@7;",
            SchemaName = "public",
            TableName = "TestCache"
        };

        // Ensure the table does not exist by dropping it (if it exists)
        await using (var conn = new NpgsqlConnection(options.ConnectionString))
        {
            await conn.OpenAsync();
            string dropTableQuery = $@"DROP TABLE IF EXISTS {options.SchemaName}.""{options.TableName}""";
            await using (var cmd = new NpgsqlCommand(dropTableQuery, conn))
            {
                await cmd.ExecuteNonQueryAsync();
            }
        }

        // Act: Perform a Set operation which should trigger auto table creation in CreateOpenConnectionAsync.
        var cache = new PostgreSqlDistributedCache(Options.Create(options));
        string key = "autoTableKey";
        byte[] value = Encoding.UTF8.GetBytes("autoTableValue");
        await cache.SetAsync(key, value, TimeSpan.FromMinutes(5));

        // Assert: Verify that the table now exists.
        await using (var conn = new NpgsqlConnection(options.ConnectionString))
        {
            await conn.OpenAsync();
            string checkTableQuery = $@"
                SELECT EXISTS (
                    SELECT FROM information_schema.tables 
                    WHERE table_schema = @schema_name 
                    AND table_name = @table_name
                )";
            await using (var cmd = new NpgsqlCommand(checkTableQuery, conn))
            {
                cmd.Parameters.AddWithValue("schema_name", options.SchemaName);
                cmd.Parameters.AddWithValue("table_name", options.TableName);
                var exists = await cmd.ExecuteScalarAsync();
                Assert.True(exists is bool b && b, "The table was not auto-created.");
            }
        }
    }
}

