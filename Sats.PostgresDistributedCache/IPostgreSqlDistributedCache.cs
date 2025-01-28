
namespace Sats.PostgresDistributedCache;

public interface IPostgreSqlDistributedCache
{
    Task<byte[]?> GetAsync(string key, CancellationToken token = default);
    Task SetAsync(string key, byte[] value, TimeSpan? expiration = null, CancellationToken token = default);
    Task RemoveAsync(string key, CancellationToken token = default);
    Task RefreshAsync(string key, CancellationToken token = default);
}
