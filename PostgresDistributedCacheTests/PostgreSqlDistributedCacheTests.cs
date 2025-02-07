

using System.Text;
using Sats.PostgreSqlDistributedCache;

namespace PostgresDistributedCacheTests;

public class PostgreSqlDistributedCacheTests
{
    private readonly string _connectionString = "Server=127.0.0.1;Port=5432;Database=HIS;User Id=postgres;Password=satsvelke@7;";

    private PostgreSqlDistributedCache CreateCache()
    {
        return new PostgreSqlDistributedCache(_connectionString);
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
}

