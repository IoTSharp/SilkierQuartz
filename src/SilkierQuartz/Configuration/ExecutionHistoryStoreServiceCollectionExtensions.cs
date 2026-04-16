using Microsoft.Extensions.DependencyInjection.Extensions;
using Quartz.Plugins.RecentHistory;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ExecutionHistoryStoreServiceCollectionExtensions
    {
        public static IServiceCollection AddExecutionHistoryStore(
            this IServiceCollection services,
            Action<ExecutionHistoryStoreOptions> configure)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var options = new ExecutionHistoryStoreOptions();
            configure(options);

            services.RemoveAll<IExecutionHistoryStore>();
            services.AddSingleton(_ => options.Build());
            return services;
        }
    }
}
