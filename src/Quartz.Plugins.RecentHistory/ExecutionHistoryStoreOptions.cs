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

        public ExecutionHistoryStoreOptions UseAdoProvider(
            string connectionString,
            DbProviderFactory providerFactory = null,
            string tablePrefix = DefaultTablePrefix)
            => Use(RelationalExecutionHistoryStoreSettings.Create(connectionString, providerFactory, tablePrefix));

        private ExecutionHistoryStoreOptions Use(RelationalExecutionHistoryStoreSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            return this;
        }
    }
}
