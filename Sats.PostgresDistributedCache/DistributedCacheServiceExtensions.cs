using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Sats.PostgresDistributedCache;

namespace Sats.PostgreSqlDistributedCache
{
    public static class DistributedCacheServiceExtensions
    {
        public static IServiceCollection AddPostgresDistributedCache(
            this IServiceCollection services, Action<PostgresDistributedCacheOptions> configureOptions)
        {
            ArgumentNullException.ThrowIfNull(configureOptions);

            var options = new PostgresDistributedCacheOptions();
            configureOptions(options);

            if (string.IsNullOrEmpty(options.ConnectionString))
            {
                throw new InvalidOperationException("PostgreSQL cache connection string is missing.");
            }

            // Register the service with dependency injection as IPostgreSqlDistributedCache
            services.AddSingleton<IPostgreSqlDistributedCache>(sp =>
                new PostgreSqlDistributedCache(options.ConnectionString!));

            // Also register as IDistributedCache for standard interface usage
            services.AddSingleton<IDistributedCache>(sp =>
                sp.GetRequiredService<IPostgreSqlDistributedCache>());

            return services;
        }

        public static IServiceCollection AddPostgresDistributedCache(
            this IServiceCollection services, Action<IServiceProvider, PostgresDistributedCacheOptions> configureOptions)
        {
            ArgumentNullException.ThrowIfNull(configureOptions);

            services.AddSingleton<IPostgreSqlDistributedCache>(sp =>
            {
                var options = new PostgresDistributedCacheOptions();
                configureOptions(sp, options);

                if (string.IsNullOrEmpty(options.ConnectionString))
                {
                    throw new InvalidOperationException("PostgreSQL cache connection string is missing.");
                }

                return new PostgreSqlDistributedCache(options.ConnectionString!);
            });

            // Also register as IDistributedCache for standard interface usage
            services.AddSingleton<IDistributedCache>(sp =>
                sp.GetRequiredService<IPostgreSqlDistributedCache>());

            return services;
        }
    }

    public class PostgresDistributedCacheOptions : IOptions<PostgresDistributedCacheOptions>
    {
        private string _connectionString;

        public TimeSpan? ExpiredItemsDeletionInterval { get; set; }

        public string ConnectionString
        {
            get => this._connectionString;
            set
            {
                this._connectionString = value;
                if (this.ReadConnectionString == null)
                    this.ReadConnectionString = value;
                if (this.WriteConnectionString != null)
                    return;
                this.WriteConnectionString = value;
            }
        }


        public string ReadConnectionString { get; set; }

        public string WriteConnectionString { get; set; }

        public string SchemaName { get; set; } = "public";

        public string TableName { get; set; } = "Cache";

        public TimeSpan DefaultSlidingExpiration { get; set; } = TimeSpan.FromMinutes(20.0);

        public PostgresDistributedCacheOptions Value
        {
            get
            {
                this.WriteConnectionString = this.WriteConnectionString ?? this.ConnectionString;
                this.ReadConnectionString = (this.ReadConnectionString ?? this.ConnectionString) ?? this.WriteConnectionString;
                return this;
            }
        }
    }
}
