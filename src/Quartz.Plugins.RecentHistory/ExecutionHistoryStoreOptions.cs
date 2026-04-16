using System;
using Quartz.Plugins.RecentHistory.Impl;

namespace Quartz.Plugins.RecentHistory
{
    public sealed class ExecutionHistoryStoreOptions
    {
        public const string DefaultTablePrefix = "tb_quartz_";

        private RelationalExecutionHistoryStoreSettings _settings;

        internal IExecutionHistoryStore Build()
        {
            if (_settings == null)
            {
                throw new InvalidOperationException("No execution history store provider has been configured.");
            }

            return new RelationalExecutionHistoryStore(_settings);
        }

        public ExecutionHistoryStoreOptions UseSqlite(string connectionString, string tablePrefix = DefaultTablePrefix)
            => Use(RelationalExecutionHistoryStoreSettings.CreateSqlite(connectionString, tablePrefix));

        public ExecutionHistoryStoreOptions UsePostgreSql(string connectionString, string tablePrefix = DefaultTablePrefix)
            => Use(RelationalExecutionHistoryStoreSettings.CreatePostgreSql(connectionString, tablePrefix));

        public ExecutionHistoryStoreOptions UsePgsql(string connectionString, string tablePrefix = DefaultTablePrefix)
            => UsePostgreSql(connectionString, tablePrefix);

        public ExecutionHistoryStoreOptions UseMySql(string connectionString, string tablePrefix = DefaultTablePrefix)
            => Use(RelationalExecutionHistoryStoreSettings.CreateMySql(connectionString, tablePrefix));

        public ExecutionHistoryStoreOptions UseSqlServer(string connectionString, string tablePrefix = DefaultTablePrefix)
            => Use(RelationalExecutionHistoryStoreSettings.CreateSqlServer(connectionString, tablePrefix));

        public ExecutionHistoryStoreOptions UseOracle(string connectionString, string tablePrefix = DefaultTablePrefix)
            => Use(RelationalExecutionHistoryStoreSettings.CreateOracle(connectionString, tablePrefix));

        private ExecutionHistoryStoreOptions Use(RelationalExecutionHistoryStoreSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            return this;
        }
    }
}
