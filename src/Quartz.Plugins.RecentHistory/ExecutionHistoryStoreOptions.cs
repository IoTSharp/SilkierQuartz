using System;
using System.Data.Common;
using Quartz.Plugins.RecentHistory.Impl;

namespace Quartz.Plugins.RecentHistory
{
    public sealed class ExecutionHistoryStoreOptions
    {
        public const string DefaultTablePrefix = "tb_quartz_";

        private RelationalExecutionHistoryStoreSettings _settings;

        public IExecutionHistoryStore Build()
        {
            if (_settings == null)
            {
                throw new InvalidOperationException("No execution history store provider has been configured.");
            }

            return new RelationalExecutionHistoryStore(_settings);
        }

        public ExecutionHistoryStoreOptions UseSqlite(string connectionString, DbProviderFactory providerFactory = null, string tablePrefix = DefaultTablePrefix)
            => Use(RelationalExecutionHistoryStoreSettings.CreateSqlite(connectionString, providerFactory, tablePrefix));

        public ExecutionHistoryStoreOptions UsePostgreSql(string connectionString, DbProviderFactory providerFactory = null, string tablePrefix = DefaultTablePrefix)
            => Use(RelationalExecutionHistoryStoreSettings.CreatePostgreSql(connectionString, providerFactory, tablePrefix));

        public ExecutionHistoryStoreOptions UsePgsql(string connectionString, DbProviderFactory providerFactory = null, string tablePrefix = DefaultTablePrefix)
            => UsePostgreSql(connectionString, providerFactory, tablePrefix);

        public ExecutionHistoryStoreOptions UseMySql(string connectionString, DbProviderFactory providerFactory = null, string tablePrefix = DefaultTablePrefix)
            => Use(RelationalExecutionHistoryStoreSettings.CreateMySql(connectionString, providerFactory, tablePrefix));

        public ExecutionHistoryStoreOptions UseSqlServer(string connectionString, DbProviderFactory providerFactory = null, string tablePrefix = DefaultTablePrefix)
            => Use(RelationalExecutionHistoryStoreSettings.CreateSqlServer(connectionString, providerFactory, tablePrefix));

        public ExecutionHistoryStoreOptions UseOracle(string connectionString, DbProviderFactory providerFactory = null, string tablePrefix = DefaultTablePrefix)
            => Use(RelationalExecutionHistoryStoreSettings.CreateOracle(connectionString, providerFactory, tablePrefix));

        public ExecutionHistoryStoreOptions UseAdoProvider(
            string providerInvariantName,
            string connectionString,
            DbProviderFactory providerFactory = null,
            string tablePrefix = DefaultTablePrefix)
            => Use(RelationalExecutionHistoryStoreSettings.Create(providerInvariantName, connectionString, providerFactory, tablePrefix));

        private ExecutionHistoryStoreOptions Use(RelationalExecutionHistoryStoreSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            return this;
        }
    }
}
