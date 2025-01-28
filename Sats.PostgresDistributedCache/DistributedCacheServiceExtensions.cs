using Microsoft.Extensions.DependencyInjection;
using Sats.PostgresDistributedCache;

namespace Sats.PostgreSqlDistributedCache
{
    public static class DistributedCacheServiceExtensions
    {
        public static IServiceCollection AddPostgresDistributedCache(
            this IServiceCollection services, Action<PostgresDistributedCacheOptions> configureOptions)
        {
            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            var options = new PostgresDistributedCacheOptions();
            configureOptions(options);

            if (string.IsNullOrEmpty(options.ConnectionString))
            {
                throw new InvalidOperationException("PostgreSQL cache connection string is missing.");
            }

            // Register the service with dependency injection
            services.AddSingleton<IPostgreSqlDistributedCache>(sp =>
                new PostgreSqlDistributedCache(options.ConnectionString!));

            return services;
        }
    }

    public class PostgresDistributedCacheOptions
    {
        public string? ConnectionString { get; set; }
    }
}
