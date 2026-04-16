using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using Quartz.Impl;
using Quartz.Plugins.RecentHistory;
using Quartz.Spi;
using SilkierQuartz.HostedService;
using System;
using System.Collections.Specialized;

namespace SilkierQuartz
{
    public static class IServiceCollectionExtensions
    {
        private static bool _quartzHostedServiceIsAdded = false;
        /// <summary>
        ///  Must be call after Host.CreateDefaultBuilder(args).ConfigureWebHostDefaults()
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IHostBuilder ConfigureSilkierQuartzHost(this IHostBuilder builder)
        {
            _quartzHostedServiceIsAdded = true;
            return builder.ConfigureServices(services => services.AddHostedService<QuartzHostedService>());
        }
        [Obsolete("We recommend ConfigureSilkierQuartzHost")]
        public static IHostBuilder ConfigureQuartzHost(this IHostBuilder builder) => builder.ConfigureSilkierQuartzHost();

        public static IJobRegistrator UseQuartzHostedService(this IServiceCollection services,
        Action<NameValueCollection> stdSchedulerFactoryOptions)
        {
            if (!_quartzHostedServiceIsAdded)
            {
                services.AddHostedService<QuartzHostedService>();
            }
            services.AddTransient<ISchedulerFactory>(provider =>
            {
                var options = new NameValueCollection();
                stdSchedulerFactoryOptions?.Invoke(options);
                if (provider.GetService<IExecutionHistoryStore>() != null
                    && string.IsNullOrWhiteSpace(options["quartz.plugin.recentHistory.type"]))
                {
                    options["quartz.plugin.recentHistory.type"] = $"{typeof(ExecutionHistoryPlugin).FullName}, {typeof(ExecutionHistoryPlugin).Assembly.GetName().Name}";
                }

                var result = new StdSchedulerFactory();
                if (options.Count > 0)
                    result.Initialize(options);
                return result;
            });
            services.AddSingleton<IJobFactory, ServiceCollectionJobFactory>();
            return new JobRegistrator(services);
        }
    }
}
