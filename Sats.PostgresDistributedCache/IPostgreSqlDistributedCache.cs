using Microsoft.Extensions.Caching.Distributed;

namespace Sats.PostgresDistributedCache;

public interface IPostgreSqlDistributedCache : IDistributedCache
{
    Task<byte[]?> GetAsync(string key, CancellationToken token = default);
    Task SetAsync(string key, byte[] value, TimeSpan? expiration = null, CancellationToken token = default);
    Task<string?> GetStringAsync(string key, CancellationToken token = default);
    Task SetStringAsync(string key, string value, TimeSpan? expiration = null, CancellationToken token = default);
    Task RemoveAsync(string key, CancellationToken token = default);
    Task RefreshAsync(string key, CancellationToken token = default);
}
