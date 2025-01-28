# Sats.PostgresDistributedCache

This is a custom distributed cache implementation using **PostgreSQL** as the backing store for caching. The cache supports basic caching operations like setting, getting, refreshing, and removing cache entries, along with support for expiration.

## Installation

To install the `Sats.PostgresDistributedCache` NuGet package, run the following command in your .NET project:

```bash
dotnet add package Sats.PostgresDistributedCache --version 1.2.0
```

Alternatively, you can use the NuGet Package Manager in Visual Studio.

## Usage

1. **Configuration**: In your `Startup.cs` or `Program.cs` (depending on your .NET version), configure the PostgreSQL distributed cache:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Add PostgreSQL distributed cache with your connection string
    services.AddPostgresDistributedCache(options =>
    {
        options.ConnectionString = "Host=myserver;Port=5432;Database=mydb;Username=myuser;Password=mypassword";
    });

    // Add other services as needed
}
```

2. **Using the Cache**: You can inject `IPostgreSqlDistributedCache` into your services or controllers and use it as follows:

```csharp
public class MyService
{
    private readonly IPostgreSqlDistributedCache _distributedCache;

    public MyService(IPostgreSqlDistributedCache distributedCache)
    {
        _distributedCache = distributedCache;
    }

    public async Task<string> GetFromCacheAsync(string key)
    {
        var data = await _distributedCache.GetAsync(key);
        return data != null ? Encoding.UTF8.GetString(data) : null;
    }

    public async Task SetInCacheAsync(string key, string value)
    {
        var data = Encoding.UTF8.GetBytes(value);
        await _distributedCache.SetAsync(key, data, expiration: TimeSpan.FromMinutes(10));
    }
}
```

## Table Schema

The cache is stored in a PostgreSQL table named `Cache` with the following schema:

```sql
CREATE TABLE IF NOT EXISTS public."Cache"
(
    "key" character varying(255) NOT NULL,
    "value" text NOT NULL,
    "expiration" timestamp with time zone,
    CONSTRAINT "PK_Cache" PRIMARY KEY ("key"),
    CONSTRAINT "UQ_Cache_Key" UNIQUE ("key")
);
```

### Columns:

- **Key**: The key of the cache entry (mapped to `Key` in the `CacheEntry` class).
- **Value**: The value of the cache entry, stored as text (mapped to `Value` in the `CacheEntry` class).
- **Expiration**: The expiration time of the cache entry (mapped to `Expiration` in the `CacheEntry` class).

## Supported .NET Versions

This package supports the following .NET versions:

- .NET 6.0
- .NET 7.0
- .NET 8.0
- .NET 9.0

Make sure your project is targeting one of these versions to use `Sats.PostgresDistributedCache`.

## License

This package is licensed under the MIT License.

