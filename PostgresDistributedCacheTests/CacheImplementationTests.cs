using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Npgsql;
using Sats.PostgresDistributedCache;
using Sats.PostgreSqlDistributedCache;
using Xunit;
using Xunit.Abstractions;

namespace PostgresDistributedCacheTests;

/// <summary>
/// This test class demonstrates three implementation patterns:
/// 1. Using the standard IDistributedCache interface
/// 2. Using PostgreSQL-specific features via IPostgreSqlDistributedCache
/// 3. Swapping between different cache implementations
/// </summary>
public class CacheImplementationTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    // Test connection string - update these with your actual PostgreSQL connection details
    private const string TestConnectionString = "Server=127.0.0.1;Port=5432;Database=HIS;User Id=postgres;Password=satsvelke@7;";
    private readonly List<string> _tablesToCleanup = new();

    public CacheImplementationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public void Dispose()
    {
        CleanupTables();
    }

    private void CleanupTables()
    {
        try
        {
            using var conn = new NpgsqlConnection(TestConnectionString);
            conn.Open();

            foreach (var tableName in _tablesToCleanup)
            {
                using var cmd = new NpgsqlCommand($"DROP TABLE IF EXISTS public.\"{tableName}\"", conn);
                cmd.ExecuteNonQuery();
                _output.WriteLine($"Cleaned up table: {tableName}");
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Error during test cleanup: {ex.Message}");
        }
    }

    private bool EnsureDatabaseConnection()
    {
        try
        {
            using var conn = new NpgsqlConnection(TestConnectionString);
            conn.Open();
            _output.WriteLine("PostgreSQL connection successful");
            return true;
        }
        catch (Exception ex)
        {
            _output.WriteLine($"PostgreSQL connection failed: {ex.Message}");
            return false;
        }
    }

    #region Standard IDistributedCache Usage

    [Fact]
    public async Task StandardDistributedCache_Basic_Operations()
    {
        // Skip test if database connection fails
        if (!EnsureDatabaseConnection())
        {
            _output.WriteLine("Test skipped due to database connection issues");
            return;
        }

        // Arrange
        string tableName = "TestStandardCache";
        _tablesToCleanup.Add(tableName); // Add to cleanup list

        // Remove table if it exists already (for clean test)
        // try
        // {
        //     using var conn = new NpgsqlConnection(TestConnectionString);
        //     await conn.OpenAsync();
        //     using var cmd = new NpgsqlCommand($"DROP TABLE IF EXISTS public.\"{tableName}\"", conn);
        //     await cmd.ExecuteNonQueryAsync();
        //     _output.WriteLine($"Cleared existing table: {tableName}");
        // }
        // catch (Exception ex)
        // {
        //     _output.WriteLine($"Error clearing table: {ex.Message}");
        // }

        var serviceCollection = new ServiceCollection();

        // Configure PostgreSQL cache as an IDistributedCache
        serviceCollection.AddPostgresDistributedCache(options =>
        {
            options.ConnectionString = TestConnectionString;
            options.TableName = tableName;
            options.SchemaName = "public";
        });

        var services = serviceCollection.BuildServiceProvider();

        // Create a standard CacheService using IDistributedCache
        var cacheService = new CacheService(services.GetRequiredService<IDistributedCache>());

        // Act & Assert
        string key = "test-key";
        string value = "test-value";

        try
        {
            // Test set operation
            await cacheService.SetValueAsync(key, value);
            _output.WriteLine($"Successfully set value for key: {key}");

            // Test get operation
            var retrievedValue = await cacheService.GetValueAsync(key);
            _output.WriteLine($"Retrieved value: {retrievedValue}");

            Assert.Equal(value, retrievedValue);
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Test failed with exception: {ex}");
            throw;
        }
    }

    /// <summary>
    /// Example service using standard IDistributedCache interface
    /// </summary>
    private class CacheService
    {
        private readonly IDistributedCache _cache;

        public CacheService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<string> GetValueAsync(string key)
        {
            var data = await _cache.GetStringAsync(key);
            return data ?? "Not found";
        }

        public async Task SetValueAsync(string key, string value)
        {
            await _cache.SetStringAsync(key, value, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
            });
        }
    }

    #endregion

    #region PostgreSQL-Specific Features

    [Fact]
    public async Task PostgreSqlSpecific_Features_Test()
    {
        // Skip test if database connection fails
        if (!EnsureDatabaseConnection())
        {
            _output.WriteLine("Test skipped due to database connection issues");
            return;
        }

        // Arrange
        string tableName = "TestPostgresSpecificCache";
        _tablesToCleanup.Add(tableName); // Add to cleanup list

        // Remove table if it exists already (for clean test)
        try
        {
            using var conn = new NpgsqlConnection(TestConnectionString);
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand($"DROP TABLE IF EXISTS public.\"{tableName}\"", conn);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Error clearing table: {ex.Message}");
        }

        var serviceCollection = new ServiceCollection();

        // Configure PostgreSQL cache
        serviceCollection.AddPostgresDistributedCache(options =>
        {
            options.ConnectionString = TestConnectionString;
            options.TableName = tableName;
            options.SchemaName = "public";
        });

        var services = serviceCollection.BuildServiceProvider();

        // Create a PostgreSQL-specific service
        var cacheService = new PostgresCacheService(
            services.GetRequiredService<IPostgreSqlDistributedCache>());

        // Act & Assert
        string key = "postgres-specific-key";
        string value = "postgres-specific-value";

        try
        {
            // Test PostgreSQL-specific set operation with custom expiration
            await cacheService.SetStringDirectlyAsync(key, value, TimeSpan.FromMinutes(45));
            _output.WriteLine($"Successfully set value for key: {key}");

            // Test PostgreSQL-specific get operation
            var retrievedValue = await cacheService.GetStringDirectlyAsync(key);
            _output.WriteLine($"Retrieved value: {retrievedValue}");

            Assert.Equal(value, retrievedValue);
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Test failed with exception: {ex}");
            throw;
        }
    }

    /// <summary>
    /// Example service using PostgreSQL-specific features
    /// </summary>
    private class PostgresCacheService
    {
        private readonly IPostgreSqlDistributedCache _cache;

        public PostgresCacheService(IPostgreSqlDistributedCache cache)
        {
            _cache = cache;
        }

        // Using PostgreSQL-specific string methods
        public async Task<string?> GetStringDirectlyAsync(string key)
        {
            return await _cache.GetStringAsync(key);
        }

        public async Task SetStringDirectlyAsync(string key, string value, TimeSpan expiration)
        {
            await _cache.SetStringAsync(key, value, expiration);
        }
    }

    #endregion

    #region Swapping Cache Implementations

    [Fact]
    public async Task Swapping_Cache_Implementations_Test()
    {
        try
        {
            // Test with both implementations to show they're interchangeable
            await TestWithCacheImplementation(usePostgresCache: false);

            // Only run PostgreSQL test if connection is available
            if (EnsureDatabaseConnection())
            {
                await TestWithCacheImplementation(usePostgresCache: true);
            }
            else
            {
                _output.WriteLine("PostgreSQL implementation test skipped due to connection issues");
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Test failed with exception: {ex}");
            throw;
        }
    }

    private async Task TestWithCacheImplementation(bool usePostgresCache)
    {
        // Arrange
        string tableName = "TestSwappableCache";

        if (usePostgresCache)
        {
            _tablesToCleanup.Add(tableName); // Add to cleanup list

            // Remove table if it exists already (for clean test)
            try
            {
                using var conn = new NpgsqlConnection(TestConnectionString);
                await conn.OpenAsync();
                using var cmd = new NpgsqlCommand($"DROP TABLE IF EXISTS public.\"{tableName}\"", conn);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Error clearing table: {ex.Message}");
            }
        }

        var serviceCollection = new ServiceCollection();

        // Create a mock configuration with dictionary
        var mockConfiguration = new Mock<IConfiguration>();
        mockConfiguration
            .Setup(c => c["UsePostgresCache"])
            .Returns(usePostgresCache.ToString());

        // Add services based on configuration
        if (usePostgresCache)
        {
            serviceCollection.AddPostgresDistributedCache(options =>
            {
                options.ConnectionString = TestConnectionString;
                options.TableName = tableName;
                options.SchemaName = "public";
            });
        }
        else
        {
            // Use in-memory cache for testing
            serviceCollection.AddDistributedMemoryCache();
        }

        // Register application service with IDistributedCache dependency
        serviceCollection.AddTransient<ApplicationService>();

        var services = serviceCollection.BuildServiceProvider();
        var appService = services.GetRequiredService<ApplicationService>();

        // Act
        string key = $"swappable-key-{(usePostgresCache ? "postgres" : "memory")}";
        string value = $"swappable-value-{(usePostgresCache ? "postgres" : "memory")}";

        _output.WriteLine($"Testing with implementation: {(usePostgresCache ? "PostgreSQL" : "Memory")}");

        await appService.StoreDataAsync(key, value);
        _output.WriteLine($"Successfully stored value for key: {key}");

        string retrievedValue = await appService.RetrieveDataAsync(key);
        _output.WriteLine($"Retrieved value: {retrievedValue}");

        // Assert
        Assert.Equal(value, retrievedValue);
    }

    /// <summary>
    /// Application service that works with any IDistributedCache implementation
    /// </summary>
    private class ApplicationService
    {
        private readonly IDistributedCache _cache;

        public ApplicationService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task StoreDataAsync(string key, string value)
        {
            await _cache.SetStringAsync(key, value);
        }

        public async Task<string> RetrieveDataAsync(string key)
        {
            var value = await _cache.GetStringAsync(key);
            return value ?? "Not found";
        }
    }

    #endregion
}