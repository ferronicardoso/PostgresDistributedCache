# Sats.PostgreSqlDistributedCache

This is a custom distributed cache implementation using **PostgreSQL** as the backing store for caching. The cache supports basic caching operations like setting, getting, refreshing, and removing cache entries, along with support for expiration.

## Installation

To install the `Sats.PostgreSqlDistributedCache` NuGet package, run the following command in your .NET project:

```bash
dotnet add package Sats.PostgreSqlDistributedCache --version 1.0.0
```

Alternatively, you can use the NuGet Package Manager in Visual Studio.

## Usage

1. **Configuration**: In your `Startup.cs` or `Program.cs` (depending on your .NET version), configure the PostgreSQL distributed cache:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Add PostgreSQL distributed cache with your connection string
    services.AddDistributedCache(options =>
    {
        options.ConnectionString = "Host=myserver;Port=5432;Database=mydb;Username=myuser;Password=mypassword";
    });

    // Add other services as needed
}
```

2. **Using the Cache**: You can inject `IDistributedCache` into your services or controllers and use it as follows:

```csharp
public class MyService
{
    private readonly IDistributedCache _distributedCache;

    public MyService(IDistributedCache distributedCache)
    {
        _distributedCache = distributedCache;
    }

    public async Task<string> GetFromCacheAsync(string key)
    {
        return await _distributedCache.GetStringAsync(key);
    }

    public async Task SetInCacheAsync(string key, string value)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = DateTime.UtcNow.AddMinutes(10)
        };

        await _distributedCache.SetStringAsync(key, value, options);
    }
}
```

## Configuration Options

- **ConnectionString**: The connection string for your PostgreSQL database.
- **Expiration**: You can set the absolute expiration for cache entries (e.g., 5 minutes, 10 minutes, etc.).

## Supported .NET Versions

This package supports the following .NET versions:


- .NET 6.0
- .NET 7.0
- .NET 8.0
- .NET 9.0

Make sure your project is targeting one of these versions to use `Sats.PostgreSqlDistributedCache`.

## License

This package is licensed under the MIT License.

