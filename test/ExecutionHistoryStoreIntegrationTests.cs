using FluentAssertions;
using Quartz.Plugins.RecentHistory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using MySqlConnector;
using Npgsql;
using Oracle.ManagedDataAccess.Client;
using Xunit;

namespace SilkierQuartz.Test
{
    public class ExecutionHistoryStoreIntegrationTests
    {
        [Fact(DisplayName = "SQLite execution history store persists and queries history")]
        public async Task Sqlite_execution_history_store_round_trips_data()
        {
            var databaseFile = Path.Combine(Path.GetTempPath(), $"silkierquartz-history-{Guid.NewGuid():N}.db");
            try
            {
                await AssertStoreBehaviorAsync(options => options.UseSqlite($"Data Source={databaseFile};Mode=ReadWriteCreate;Cache=Shared", SqliteFactory.Instance));
            }
            finally
            {
                if (File.Exists(databaseFile))
                {
                    File.Delete(databaseFile);
                }
            }
        }

        [Fact(DisplayName = "PostgreSQL execution history store persists and queries history", Timeout = 300000)]
        public async Task PostgreSql_execution_history_store_round_trips_data()
        {
            await RunContainerStoreTestAsync(
                image: "postgres:16-alpine",
                port: 5432,
                environment: new Dictionary<string, string>
                {
                    ["POSTGRES_USER"] = "postgres",
                    ["POSTGRES_PASSWORD"] = "Password123!",
                    ["POSTGRES_DB"] = "silkierquartz"
                },
                configureStore: (options, host, mappedPort) =>
                    options.UseAdoProvider("Npgsql", $"Host={host};Port={mappedPort};Database=silkierquartz;Username=postgres;Password=Password123!;Pooling=false", NpgsqlFactory.Instance));
        }

        [Fact(DisplayName = "MySQL execution history store persists and queries history", Timeout = 300000)]
        public async Task MySql_execution_history_store_round_trips_data()
        {
            await RunContainerStoreTestAsync(
                image: "mysql:8.4",
                port: 3306,
                environment: new Dictionary<string, string>
                {
                    ["MYSQL_ROOT_PASSWORD"] = "Password123!",
                    ["MYSQL_DATABASE"] = "silkierquartz"
                },
                configureStore: (options, host, mappedPort) =>
                    options.UseAdoProvider("MySqlConnector", $"Server={host};Port={mappedPort};Database=silkierquartz;User ID=root;Password=Password123!;SslMode=None;AllowPublicKeyRetrieval=True", MySqlConnectorFactory.Instance));
        }

        [Fact(DisplayName = "SQL Server execution history store persists and queries history", Timeout = 300000)]
        public async Task SqlServer_execution_history_store_round_trips_data()
        {
            await RunContainerStoreTestAsync(
                image: "mcr.microsoft.com/mssql/server:2022-latest",
                port: 1433,
                environment: new Dictionary<string, string>
                {
                    ["ACCEPT_EULA"] = "Y",
                    ["MSSQL_SA_PASSWORD"] = "Password123!Aa"
                },
                configureStore: (options, host, mappedPort) =>
                    options.UseAdoProvider("Microsoft.Data.SqlClient", $"Server={host},{mappedPort};Database=master;User ID=sa;Password=Password123!Aa;Encrypt=False;TrustServerCertificate=True", SqlClientFactory.Instance));
        }

        [Fact(DisplayName = "Oracle execution history store persists and queries history", Timeout = 600000)]
        public async Task Oracle_execution_history_store_round_trips_data()
        {
            await RunContainerStoreTestAsync(
                image: "gvenzl/oracle-free:23-slim-faststart",
                port: 1521,
                environment: new Dictionary<string, string>
                {
                    ["ORACLE_PASSWORD"] = "Password123!"
                },
                configureStore: (options, host, mappedPort) =>
                    options.UseAdoProvider("Oracle.ManagedDataAccess.Client", $"User Id=system;Password=Password123!;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={host})(PORT={mappedPort}))(CONNECT_DATA=(SERVICE_NAME=FREEPDB1)))", OracleClientFactory.Instance));
        }

        private static async Task RunContainerStoreTestAsync(
            string image,
            ushort port,
            IReadOnlyDictionary<string, string> environment,
            Action<ExecutionHistoryStoreOptions, string, ushort> configureStore)
        {
            var builder = new ContainerBuilder()
                .WithImage(image)
                .WithCleanUp(true)
                .WithPortBinding(port, true)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(port));

            foreach (var item in environment)
            {
                builder = builder.WithEnvironment(item.Key, item.Value);
            }

            await using var container = builder.Build();
            await container.StartAsync();

            var host = container.Hostname;
            var mappedPort = container.GetMappedPublicPort(port);

            await AssertStoreBehaviorAsync(options => configureStore(options, host, mappedPort));
        }

        private static async Task AssertStoreBehaviorAsync(Action<ExecutionHistoryStoreOptions> configureStore)
        {
            var options = new ExecutionHistoryStoreOptions();
            configureStore(options);

            Exception lastException = null;
            for (var attempt = 0; attempt < 30; attempt++)
            {
                try
                {
                    var store = options.Build();
                    await AssertStoreBehaviorAsync(store);
                    return;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }
            }

            throw lastException ?? new InvalidOperationException("The database container did not become ready in time.");
        }

        private static async Task AssertStoreBehaviorAsync(IExecutionHistoryStore store)
        {
            store.SchedulerName = "scheduler-a";

            var firstTriggerFirstJob = CreateEntry("fire-1", "job-a", "trigger-a", 1);
            var secondTriggerFirstJob = CreateEntry("fire-2", "job-a", "trigger-a", 2);
            var differentTrigger = CreateEntry("fire-3", "job-b", "trigger-b", 3);
            var otherScheduler = CreateEntry("fire-4", "job-c", "trigger-c", 4, "scheduler-b");

            await store.Save(firstTriggerFirstJob);
            await store.Save(secondTriggerFirstJob);
            await store.Save(differentTrigger);
            await store.Save(otherScheduler);

            var updatedSecondEntry = CreateEntry("fire-2", "job-a", "trigger-a", 2);
            updatedSecondEntry.FinishedTimeUtc = updatedSecondEntry.ActualFireTimeUtc.AddSeconds(30);
            updatedSecondEntry.ExceptionMessage = "boom";
            await store.Save(updatedSecondEntry);

            (await store.Get("fire-2")).Should().BeEquivalentTo(updatedSecondEntry);
            (await store.Get("missing")).Should().BeNull();

            (await store.FilterLast(10)).Select(x => x.FireInstanceId)
                .Should().Equal("fire-1", "fire-2", "fire-3");

            (await store.FilterLast(2)).Select(x => x.FireInstanceId)
                .Should().Equal("fire-2", "fire-3");

            (await store.FilterLastOfEveryJob(1)).Select(x => x.FireInstanceId)
                .Should().Equal("fire-2", "fire-3");

            (await store.FilterLastOfEveryTrigger(1)).Select(x => x.FireInstanceId)
                .Should().Equal("fire-2", "fire-3");

            await store.IncrementTotalJobsExecuted();
            await store.IncrementTotalJobsExecuted();
            await store.IncrementTotalJobsFailed();

            (await store.GetTotalJobsExecuted()).Should().Be(2);
            (await store.GetTotalJobsFailed()).Should().Be(1);

            for (var index = 5; index <= 16; index++)
            {
                await store.Save(CreateEntry($"purge-{index}", "job-a", "trigger-a", index));
            }

            var remaining = await store.FilterLastOfEveryTrigger(20);
            remaining.Select(x => x.FireInstanceId)
                .Should().HaveCount(11)
                .And.NotContain("fire-1")
                .And.Contain("purge-16");
        }

        private static ExecutionHistoryEntry CreateEntry(string fireInstanceId, string job, string trigger, int sequence, string schedulerName = "scheduler-a")
        {
            var actualFireTime = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero).AddMinutes(sequence);
            return new ExecutionHistoryEntry
            {
                FireInstanceId = fireInstanceId,
                SchedulerInstanceId = "instance-1",
                SchedulerName = schedulerName,
                Job = job,
                Trigger = trigger,
                ScheduledFireTimeUtc = actualFireTime.AddSeconds(-5),
                ActualFireTimeUtc = actualFireTime,
                Recovering = sequence % 2 == 0,
                Vetoed = false
            };
        }
    }
}
