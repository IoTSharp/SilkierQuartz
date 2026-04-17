using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using Quartz.Impl;
using Quartz.Plugins.RecentHistory;
using Quartz.Spi;
using SilkierQuartz;
using SilkierQuartz.HostedService;
using System;
using System.IO;
using Xunit;

namespace SilkierQuartz.Test
{
    public class IServiceCollectionExtensionsUnitTest
    {
        [Fact(DisplayName = "Registered HostedService")]
        public void IServiceCollectionExtensions_Register_HostedService()
        {
            IServiceCollection serviceCollection = new ServiceCollection();
            IServiceCollectionExtensions.UseQuartzHostedService(serviceCollection, null);

            var testClass = serviceCollection.BuildServiceProvider().GetRequiredService<IHostedService>();
            testClass.Should()
                .NotBeNull()
                .And.BeOfType<SilkierQuartz.HostedService.QuartzHostedService>();
        }

        [Fact(DisplayName = "IJobFactory(d - di in Job)")]
        public void IServiceCollectionExtensions_Register_IJobFactory()
        {
            IServiceCollection serviceCollection = new ServiceCollection();
            IServiceCollectionExtensions.UseQuartzHostedService(serviceCollection, null);

            var testClass = serviceCollection.BuildServiceProvider().GetRequiredService<IJobFactory>();
            testClass.Should()
                .NotBeNull()
                .And.BeOfType<ServiceCollectionJobFactory>();
        }

        [Fact(DisplayName = "ISchedulerFactory(did not pass the parameters for initialization)")]
        public void IServiceCollectionExtensions_Register_ISchedulerFactory()
        {
            IServiceCollection serviceCollection = new ServiceCollection();
            IServiceCollectionExtensions.UseQuartzHostedService(serviceCollection, null);

            var testClass = serviceCollection.BuildServiceProvider().GetRequiredService<ISchedulerFactory>();
            testClass.Should()
                .NotBeNull()
                .And.BeOfType<StdSchedulerFactory>();
        }

        [Fact(DisplayName = "ISchedulerFactory(transmitted parameters for initialization)")]
        public void IServiceCollectionExtensions_Register_ISchedulerFactory_WithParams()
        {
            IServiceCollection serviceCollection = new ServiceCollection();
            IServiceCollectionExtensions.UseQuartzHostedService(serviceCollection, options => { options.Add("quartz.threadPool.threadCount", "1"); });

            // TODO: ѕроверить что параметры передались в конструктор
            var testClass = serviceCollection.BuildServiceProvider().GetRequiredService<ISchedulerFactory>();
            testClass.Should()
                .NotBeNull()
                .And.BeOfType<StdSchedulerFactory>();
        }

        [Fact(DisplayName = "IJobRegistrator registration")]
        public void IServiceCollectionExtensions_Return_IJobRegistrator()
        {
            IServiceCollection serviceCollection = new ServiceCollection();
            var result = IServiceCollectionExtensions.UseQuartzHostedService(serviceCollection, null);

            result.Should()
                .NotBeNull()
                .And.BeAssignableTo<IJobRegistrator>()
                .Subject.Services.Should().Equal(serviceCollection);
        }

        [Fact(DisplayName = "AddExecutionHistoryStore registers relational store")]
        public void IServiceCollectionExtensions_Register_ExecutionHistoryStore()
        {
            var databaseFile = Path.Combine(Path.GetTempPath(), $"silkierquartz-history-{Guid.NewGuid():N}.db");
            try
            {
                IServiceCollection serviceCollection = new ServiceCollection();
                serviceCollection.AddExecutionHistoryStore(setting =>
                    setting.UseAdoProvider(
                        $"Data Source={databaseFile};Mode=ReadWriteCreate;Cache=Shared",
                        SqliteFactory.Instance));

                var store = serviceCollection.BuildServiceProvider().GetRequiredService<IExecutionHistoryStore>();
                store.Should().BeOfType<Quartz.Plugins.RecentHistory.Impl.RelationalExecutionHistoryStore>();
            }
            finally
            {
                if (File.Exists(databaseFile))
                {
                    File.Delete(databaseFile);
                }
            }
        }
    }
}
