using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Quartz.Plugins.RecentHistory;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Registers the EF Core execution history store for SilkierQuartz.
    /// </summary>
    public static class ExecutionHistoryStoreServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the EF Core execution history store using the provided DbContext configuration.
        /// </summary>
        public static IServiceCollection AddEfCoreExecutionHistoryStore(
            this IServiceCollection services,
            Action<DbContextOptionsBuilder> configureDbContext)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configureDbContext);

            return services.AddEfCoreExecutionHistoryStore((_, options) => configureDbContext(options));
        }

        /// <summary>
        /// Registers the EF Core execution history store using the provided service-aware DbContext configuration.
        /// </summary>
        public static IServiceCollection AddEfCoreExecutionHistoryStore(
            this IServiceCollection services,
            Action<IServiceProvider, DbContextOptionsBuilder> configureDbContext)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configureDbContext);

            services.AddDbContextFactory<Quartz.Plugins.RecentHistory.EFCore.ExecutionHistoryDbContext>(configureDbContext);
            services.RemoveAll<IExecutionHistoryStore>();
            services.AddSingleton<IExecutionHistoryStore, Quartz.Plugins.RecentHistory.EFCore.EfCoreExecutionHistoryStore>();
            return services;
        }
    }
}
