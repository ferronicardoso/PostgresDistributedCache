# 🗄️ Sats.PostgresDistributedCache

![NuGet Version](https://img.shields.io/nuget/v/Sats.PostgresDistributedCache)

![Downloads](https://img.shields.io/nuget/dt/Sats.PostgresDistributedCache)

![License](https://img.shields.io/github/license/satsvelke/PostgresDistributedCache)

![.NET](https://img.shields.io/badge/dynamic/xml?color=512BD4&label=target&query=%2F%2FTargetFrameworks&url=https://raw.githubusercontent.com/satsvelke/PostgresDistributedCache/main/Sats.PostgresDistributedCache/Sats.PostgresDistributedCache.csproj)

A high-performance distributed cache implementation using **PostgreSQL** as the backing store, fully compatible with the standard `IDistributedCache` interface.

## 🌟 Features

- ✅ Full implementation of `IDistributedCache` interface
- ✅ PostgreSQL-specific string operations for easier string caching
- ✅ Automatic table creation
- ✅ Support for expiration time
- ✅ Thread-safe operations
- ✅ Asynchronous and synchronous API support
- ✅ Easily swappable with other distributed cache implementations

## 📦 Installation

```bash
dotnet add package Sats.PostgresDistributedCache --version 1.4.0
```

Or use the Package Manager Console:

```powershell
Install-Package Sats.PostgresDistributedCache -Version 1.4.0
```

## 🚀 Getting Started

### Configuration

Configure the PostgreSQL distributed cache in your `Program.cs` (for .NET 6+ minimal hosting) or `Startup.cs`:

```csharp
// .NET 6+ Minimal API
builder.Services.AddPostgresDistributedCache(options =>
{
    options.ConnectionString = "Host=myserver;Port=5432;Database=mydb;Username=myuser;Password=mypassword";
    options.SchemaName = "public"; // Optional: defaults to "public"
    options.TableName = "Cache";   // Optional: defaults to "Cache"
});
```

### Usage with IDistributedCache

The library now implements the standard `IDistributedCache` interface, making it fully compatible with existing code that uses Microsoft's distributed cache abstractions:

```csharp
// Using standard IDistributedCache interface
public class CacheService
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
```

### PostgreSQL-Specific Features

When you need PostgreSQL-specific functionality, use the `IPostgreSqlDistributedCache` interface:

```csharp
public class PostgresCacheService
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
```

## 📊 Swapping Implementations

Thanks to implementing the standard `IDistributedCache` interface, you can easily swap between different cache implementations:

```csharp
// Choose cache implementation based on configuration
bool usePostgresCache = Configuration.GetValue<bool>("UsePostgresCache");

if (usePostgresCache)
{
    services.AddPostgresDistributedCache(options =>
    {
        options.ConnectionString = Configuration.GetConnectionString("PostgresCache");
    });
}
else
{
    // Fall back to Redis or Memory cache
    services.AddDistributedMemoryCache();
    // OR
    // services.AddStackExchangeRedisCache(options => { ... });
}

// Your application code using IDistributedCache will work with any implementation
```

## 🏗️ Database Schema

The cache automatically creates a PostgreSQL table with the following schema:

```sql
CREATE TABLE IF NOT EXISTS public."Cache" (
    key character varying(255) NOT NULL,
    value bytea NOT NULL,
    expiration timestamp with time zone,
    CONSTRAINT pk_Cache PRIMARY KEY (key),
    CONSTRAINT uq_Cache_key UNIQUE (key)  
);
```

## 🔍 Advanced Configuration Options

The `PostgresDistributedCacheOptions` class offers additional configuration options:

| Option                     | Description                        | Default                  |
| -------------------------- | ---------------------------------- | ------------------------ |
| `ConnectionString`         | Main connection string             | *Required*               |
| `ReadConnectionString`     | Optional separate read connection  | Same as ConnectionString |
| `WriteConnectionString`    | Optional separate write connection | Same as ConnectionString |
| `SchemaName`               | PostgreSQL schema name             | "public"                 |
| `TableName`                | Cache table name                   | "Cache"                  |
| `DefaultSlidingExpiration` | Default sliding expiration time    | 20 minutes               |

## 🧰 Supported .NET Versions

- .NET 6.0
- .NET 7.0
- .NET 8.0
- .NET 9.0

## 📚 API Reference

### IPostgreSqlDistributedCache Interface

```csharp
public interface IPostgreSqlDistributedCache : IDistributedCache
{
    Task<byte[]?> GetAsync(string key, CancellationToken token = default);
    Task SetAsync(string key, byte[] value, TimeSpan? expiration = null, CancellationToken token = default);
    Task<string?> GetStringAsync(string key, CancellationToken token = default);
    Task SetStringAsync(string key, string value, TimeSpan? expiration = null, CancellationToken token = default);
    Task RemoveAsync(string key, CancellationToken token = default);
    Task RefreshAsync(string key, CancellationToken token = default);
}
```

## 📝 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🤝 Contributing

Contributions are welcome! Feel free to open issues or submit pull requests.

## 📧 Contact

For questions or feedback, please open an issue on the GitHub repository.