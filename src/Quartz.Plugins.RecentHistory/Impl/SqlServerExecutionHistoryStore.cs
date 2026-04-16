using System;

namespace Quartz.Plugins.RecentHistory.Impl
{
    [Obsolete("Use AddExecutionHistoryStore(setting => setting.UseSqlServer(connectionString)) instead.")]
    [Serializable]
    public class SqlServerExecutionHistoryStore : RelationalExecutionHistoryStore
    {
        public SqlServerExecutionHistoryStore(string connectionString, string tablePrefix = ExecutionHistoryStoreOptions.DefaultTablePrefix)
            : base(RelationalExecutionHistoryStoreSettings.CreateSqlServer(connectionString, tablePrefix))
        {
        }
    }
}
