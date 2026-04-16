using System;
using System.Data.Common;

namespace Quartz.Plugins.RecentHistory.Impl
{
    [Obsolete("Use AddExecutionHistoryStore(setting => setting.UseSqlServer(connectionString)) instead.")]
    [Serializable]
    public class SqlServerExecutionHistoryStore : RelationalExecutionHistoryStore
    {
        public SqlServerExecutionHistoryStore(
            string connectionString,
            DbProviderFactory providerFactory = null,
            string tablePrefix = ExecutionHistoryStoreOptions.DefaultTablePrefix)
            : base(RelationalExecutionHistoryStoreSettings.CreateSqlServer(connectionString, providerFactory, tablePrefix))
        {
        }
    }
}
