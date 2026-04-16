using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Quartz.Plugins.RecentHistory.Impl
{
    internal sealed class RelationalExecutionHistoryStoreSettings
    {
        public string ProviderInvariantName { get; }
        public DbProviderFactory ProviderFactory { get; }
        public string ConnectionString { get; }
        public string TablePrefix { get; }

        private RelationalExecutionHistoryStoreSettings(
            string providerInvariantName,
            string connectionString,
            DbProviderFactory providerFactory,
            string tablePrefix)
        {
            if (string.IsNullOrWhiteSpace(providerInvariantName))
            {
                throw new ArgumentException("An ADO.NET provider invariant name is required.", nameof(providerInvariantName));
            }

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("A database connection string is required.", nameof(connectionString));
            }

            ProviderInvariantName = providerInvariantName.Trim();
            ProviderFactory = providerFactory;
            ConnectionString = connectionString;
            TablePrefix = string.IsNullOrWhiteSpace(tablePrefix) ? ExecutionHistoryStoreOptions.DefaultTablePrefix : tablePrefix.Trim();
        }

        public static RelationalExecutionHistoryStoreSettings Create(
            string providerInvariantName,
            string connectionString,
            DbProviderFactory providerFactory,
            string tablePrefix)
            => new RelationalExecutionHistoryStoreSettings(providerInvariantName, connectionString, providerFactory, tablePrefix);

        public static RelationalExecutionHistoryStoreSettings CreateSqlite(string connectionString, DbProviderFactory providerFactory, string tablePrefix)
            => Create("Microsoft.Data.Sqlite", connectionString, providerFactory, tablePrefix);

        public static RelationalExecutionHistoryStoreSettings CreatePostgreSql(string connectionString, DbProviderFactory providerFactory, string tablePrefix)
            => Create("Npgsql", connectionString, providerFactory, tablePrefix);

        public static RelationalExecutionHistoryStoreSettings CreateMySql(string connectionString, DbProviderFactory providerFactory, string tablePrefix)
            => Create("MySqlConnector", connectionString, providerFactory, tablePrefix);

        public static RelationalExecutionHistoryStoreSettings CreateSqlServer(string connectionString, DbProviderFactory providerFactory, string tablePrefix)
            => Create("Microsoft.Data.SqlClient", connectionString, providerFactory, tablePrefix);

        public static RelationalExecutionHistoryStoreSettings CreateOracle(string connectionString, DbProviderFactory providerFactory, string tablePrefix)
            => Create("Oracle.ManagedDataAccess.Client", connectionString, providerFactory, tablePrefix);
    }

    [Serializable]
    public class RelationalExecutionHistoryStore : IExecutionHistoryStore
    {
        private const int StatsId = 1;

        private readonly RelationalExecutionHistoryStoreSettings _settings;
        private readonly RelationalExecutionHistoryStoreDialect _dialect;
        private readonly SemaphoreSlim _initializationLock = new SemaphoreSlim(1, 1);

        private volatile bool _initialized;
        private DateTime _nextPurgeTime = DateTime.UtcNow;
        private int _updatesFromLastPurge;

        internal RelationalExecutionHistoryStore(RelationalExecutionHistoryStoreSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _dialect = RelationalExecutionHistoryStoreDialect.Create(settings);
        }

        public string SchedulerName { get; set; }

        public async Task<ExecutionHistoryEntry> Get(string fireInstanceId)
        {
            await EnsureInitializedAsync();

            await using var connection = await OpenConnectionAsync();
            await using var command = CreateCommand(connection, $@"
SELECT
    fire_instance_id,
    scheduler_instance_id,
    scheduler_name,
    job_name,
    trigger_name,
    scheduled_fire_time_utc,
    actual_fire_time_utc,
    recovering,
    vetoed,
    finished_time_utc,
    exception_message
FROM {_dialect.ExecutionHistoryTableName}
WHERE fire_instance_id = {Parameter("FireInstanceId")};");
            AddParameter(command, "FireInstanceId", fireInstanceId, DbType.String);

            await using var reader = await command.ExecuteReaderAsync();
            return await reader.ReadAsync() ? ReadEntry(reader) : null;
        }

        public async Task Save(ExecutionHistoryEntry entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            await EnsureInitializedAsync();

            _updatesFromLastPurge++;
            if (_updatesFromLastPurge >= 10 || _nextPurgeTime < DateTime.UtcNow)
            {
                _nextPurgeTime = DateTime.UtcNow.AddMinutes(1);
                _updatesFromLastPurge = 0;
                await Purge();
            }

            await using var connection = await OpenConnectionAsync();

            await using var updateCommand = CreateCommand(connection, $@"
UPDATE {_dialect.ExecutionHistoryTableName}
SET
    scheduler_instance_id = {Parameter("SchedulerInstanceId")},
    scheduler_name = {Parameter("SchedulerName")},
    job_name = {Parameter("Job")},
    trigger_name = {Parameter("Trigger")},
    scheduled_fire_time_utc = {Parameter("ScheduledFireTimeUtc")},
    actual_fire_time_utc = {Parameter("ActualFireTimeUtc")},
    recovering = {Parameter("Recovering")},
    vetoed = {Parameter("Vetoed")},
    finished_time_utc = {Parameter("FinishedTimeUtc")},
    exception_message = {Parameter("ExceptionMessage")}
WHERE fire_instance_id = {Parameter("FireInstanceId")};");
            AddEntryParameters(updateCommand, entry);

            var updatedRows = await updateCommand.ExecuteNonQueryAsync();
            if (updatedRows > 0)
            {
                return;
            }

            await using var insertCommand = CreateCommand(connection, $@"
INSERT INTO {_dialect.ExecutionHistoryTableName} (
    fire_instance_id,
    scheduler_instance_id,
    scheduler_name,
    job_name,
    trigger_name,
    scheduled_fire_time_utc,
    actual_fire_time_utc,
    recovering,
    vetoed,
    finished_time_utc,
    exception_message
) VALUES (
    {Parameter("FireInstanceId")},
    {Parameter("SchedulerInstanceId")},
    {Parameter("SchedulerName")},
    {Parameter("Job")},
    {Parameter("Trigger")},
    {Parameter("ScheduledFireTimeUtc")},
    {Parameter("ActualFireTimeUtc")},
    {Parameter("Recovering")},
    {Parameter("Vetoed")},
    {Parameter("FinishedTimeUtc")},
    {Parameter("ExceptionMessage")}
);");
            AddEntryParameters(insertCommand, entry);
            await insertCommand.ExecuteNonQueryAsync();
        }

        public async Task Purge()
        {
            await EnsureInitializedAsync();

            var ids = (await FilterLastOfEveryTrigger(10)).Select(x => x.FireInstanceId).ToArray();

            await using var connection = await OpenConnectionAsync();
            var sql = $@"DELETE FROM {_dialect.ExecutionHistoryTableName} WHERE scheduler_name = {Parameter("SchedulerName")}";
            if (ids.Length > 0)
            {
                sql += $" AND fire_instance_id NOT IN ({CreateParameterList("PurgeId", ids.Length)})";
            }

            await using var command = CreateCommand(connection, sql + ";");
            AddParameter(command, "SchedulerName", SchedulerName, DbType.String);
            for (var i = 0; i < ids.Length; i++)
            {
                AddParameter(command, $"PurgeId{i}", ids[i], DbType.String);
            }

            await command.ExecuteNonQueryAsync();
        }

        public Task<IEnumerable<ExecutionHistoryEntry>> FilterLastOfEveryJob(int limitPerJob)
            => FilterAsync($@"
SELECT
    fire_instance_id,
    scheduler_instance_id,
    scheduler_name,
    job_name,
    trigger_name,
    scheduled_fire_time_utc,
    actual_fire_time_utc,
    recovering,
    vetoed,
    finished_time_utc,
    exception_message
FROM (
    SELECT
        fire_instance_id,
        scheduler_instance_id,
        scheduler_name,
        job_name,
        trigger_name,
        scheduled_fire_time_utc,
        actual_fire_time_utc,
        recovering,
        vetoed,
        finished_time_utc,
        exception_message,
        ROW_NUMBER() OVER (PARTITION BY job_name ORDER BY actual_fire_time_utc DESC) AS row_num
    FROM {_dialect.ExecutionHistoryTableName}
    WHERE scheduler_name = {Parameter("SchedulerName")}
) history
WHERE row_num <= {Parameter("Limit")}
ORDER BY job_name, actual_fire_time_utc ASC;", limitPerJob);

        public Task<IEnumerable<ExecutionHistoryEntry>> FilterLastOfEveryTrigger(int limitPerTrigger)
            => FilterAsync($@"
SELECT
    fire_instance_id,
    scheduler_instance_id,
    scheduler_name,
    job_name,
    trigger_name,
    scheduled_fire_time_utc,
    actual_fire_time_utc,
    recovering,
    vetoed,
    finished_time_utc,
    exception_message
FROM (
    SELECT
        fire_instance_id,
        scheduler_instance_id,
        scheduler_name,
        job_name,
        trigger_name,
        scheduled_fire_time_utc,
        actual_fire_time_utc,
        recovering,
        vetoed,
        finished_time_utc,
        exception_message,
        ROW_NUMBER() OVER (PARTITION BY trigger_name ORDER BY actual_fire_time_utc DESC) AS row_num
    FROM {_dialect.ExecutionHistoryTableName}
    WHERE scheduler_name = {Parameter("SchedulerName")}
) history
WHERE row_num <= {Parameter("Limit")}
ORDER BY trigger_name, actual_fire_time_utc ASC;", limitPerTrigger);

        public Task<IEnumerable<ExecutionHistoryEntry>> FilterLast(int limit)
            => FilterAsync($@"
SELECT
    fire_instance_id,
    scheduler_instance_id,
    scheduler_name,
    job_name,
    trigger_name,
    scheduled_fire_time_utc,
    actual_fire_time_utc,
    recovering,
    vetoed,
    finished_time_utc,
    exception_message
FROM (
    SELECT
        fire_instance_id,
        scheduler_instance_id,
        scheduler_name,
        job_name,
        trigger_name,
        scheduled_fire_time_utc,
        actual_fire_time_utc,
        recovering,
        vetoed,
        finished_time_utc,
        exception_message,
        ROW_NUMBER() OVER (ORDER BY actual_fire_time_utc DESC) AS row_num
    FROM {_dialect.ExecutionHistoryTableName}
    WHERE scheduler_name = {Parameter("SchedulerName")}
) history
WHERE row_num <= {Parameter("Limit")}
ORDER BY actual_fire_time_utc ASC;", limit);

        public async Task<int> GetTotalJobsExecuted()
        {
            await EnsureInitializedAsync();

            await using var connection = await OpenConnectionAsync();
            await using var command = CreateCommand(connection, $@"
SELECT total_jobs_executed
FROM {_dialect.JobStatsTableName}
WHERE id = {Parameter("Id")};");
            AddParameter(command, "Id", StatsId, DbType.Int32);
            return await ExecuteScalarIntAsync(command);
        }

        public async Task<int> GetTotalJobsFailed()
        {
            await EnsureInitializedAsync();

            await using var connection = await OpenConnectionAsync();
            await using var command = CreateCommand(connection, $@"
SELECT total_jobs_failed
FROM {_dialect.JobStatsTableName}
WHERE id = {Parameter("Id")};");
            AddParameter(command, "Id", StatsId, DbType.Int32);
            return await ExecuteScalarIntAsync(command);
        }

        public async Task IncrementTotalJobsExecuted()
        {
            await EnsureInitializedAsync();

            await using var connection = await OpenConnectionAsync();
            await using var command = CreateCommand(connection, $@"
UPDATE {_dialect.JobStatsTableName}
SET total_jobs_executed = total_jobs_executed + 1
WHERE id = {Parameter("Id")};");
            AddParameter(command, "Id", StatsId, DbType.Int32);
            await command.ExecuteNonQueryAsync();
        }

        public async Task IncrementTotalJobsFailed()
        {
            await EnsureInitializedAsync();

            await using var connection = await OpenConnectionAsync();
            await using var command = CreateCommand(connection, $@"
UPDATE {_dialect.JobStatsTableName}
SET total_jobs_failed = total_jobs_failed + 1
WHERE id = {Parameter("Id")};");
            AddParameter(command, "Id", StatsId, DbType.Int32);
            await command.ExecuteNonQueryAsync();
        }

        private async Task<IEnumerable<ExecutionHistoryEntry>> FilterAsync(string sql, int limit)
        {
            await EnsureInitializedAsync();

            await using var connection = await OpenConnectionAsync();
            await using var command = CreateCommand(connection, sql);
            AddParameter(command, "SchedulerName", SchedulerName, DbType.String);
            AddParameter(command, "Limit", limit, DbType.Int32);

            var results = new List<ExecutionHistoryEntry>();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(ReadEntry(reader));
            }

            return results;
        }

        private async Task EnsureInitializedAsync()
        {
            if (_initialized)
            {
                return;
            }

            await _initializationLock.WaitAsync();
            try
            {
                if (_initialized)
                {
                    return;
                }

                await using var connection = await OpenConnectionAsync();
                await EnsureTableAsync(connection, _dialect.ExecutionHistoryTableName, _dialect.ExecutionHistoryTableExistsSql, _dialect.CreateExecutionHistoryTableSql);
                await EnsureTableAsync(connection, _dialect.JobStatsTableName, _dialect.JobStatsTableExistsSql, _dialect.CreateJobStatsTableSql);

                await using var statsCommand = CreateCommand(connection, $@"
SELECT COUNT(1)
FROM {_dialect.JobStatsTableName}
WHERE id = {Parameter("Id")};");
                AddParameter(statsCommand, "Id", StatsId, DbType.Int32);

                if (await ExecuteScalarIntAsync(statsCommand) == 0)
                {
                    await using var insertStatsCommand = CreateCommand(connection, $@"
INSERT INTO {_dialect.JobStatsTableName} (id, total_jobs_executed, total_jobs_failed)
VALUES ({Parameter("Id")}, 0, 0);");
                    AddParameter(insertStatsCommand, "Id", StatsId, DbType.Int32);
                    await insertStatsCommand.ExecuteNonQueryAsync();
                }

                _initialized = true;
            }
            finally
            {
                _initializationLock.Release();
            }
        }

        private async Task EnsureTableAsync(DbConnection connection, string tableName, string tableExistsSql, string createTableSql)
        {
            await using var existsCommand = CreateCommand(connection, tableExistsSql);
            AddParameter(existsCommand, "TableName", tableName, DbType.String);
            if (_dialect.RequiresUpperCaseTableNameParameter)
            {
                existsCommand.Parameters[0].Value = tableName.ToUpperInvariant();
            }

            if (await ExecuteScalarIntAsync(existsCommand) > 0)
            {
                return;
            }

            await using var createCommand = CreateCommand(connection, createTableSql);
            await createCommand.ExecuteNonQueryAsync();
        }

        private async Task<DbConnection> OpenConnectionAsync()
        {
            var providerFactory = _settings.ProviderFactory;
            if (providerFactory == null)
            {
                try
                {
                    providerFactory = DbProviderFactories.GetFactory(_settings.ProviderInvariantName);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"No ADO.NET provider factory was available for '{_settings.ProviderInvariantName}'. Pass a DbProviderFactory to AddExecutionHistoryStore or register the provider with DbProviderFactories.",
                        ex);
                }
            }

            var connection = providerFactory.CreateConnection()
                ?? throw new InvalidOperationException($"The ADO.NET provider '{_settings.ProviderInvariantName}' could not create a DbConnection instance.");
            connection.ConnectionString = _settings.ConnectionString;
            await connection.OpenAsync();
            return connection;
        }

        private DbCommand CreateCommand(DbConnection connection, string sql)
        {
            var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandType = CommandType.Text;
            if (_dialect.IsOracle)
            {
                var bindByName = command.GetType().GetProperty("BindByName");
                bindByName?.SetValue(command, true);
            }

            return command;
        }

        private void AddEntryParameters(DbCommand command, ExecutionHistoryEntry entry)
        {
            AddParameter(command, "FireInstanceId", entry.FireInstanceId, DbType.String);
            AddParameter(command, "SchedulerInstanceId", entry.SchedulerInstanceId, DbType.String);
            AddParameter(command, "SchedulerName", entry.SchedulerName, DbType.String);
            AddParameter(command, "Job", entry.Job, DbType.String);
            AddParameter(command, "Trigger", entry.Trigger, DbType.String);
            AddParameter(command, "ScheduledFireTimeUtc", FormatTimestamp(entry.ScheduledFireTimeUtc), DbType.String);
            AddParameter(command, "ActualFireTimeUtc", FormatTimestamp(entry.ActualFireTimeUtc), DbType.String);
            AddParameter(command, "Recovering", entry.Recovering ? 1 : 0, DbType.Int32);
            AddParameter(command, "Vetoed", entry.Vetoed ? 1 : 0, DbType.Int32);
            AddParameter(command, "FinishedTimeUtc", FormatTimestamp(entry.FinishedTimeUtc), DbType.String);
            AddParameter(command, "ExceptionMessage", entry.ExceptionMessage, DbType.String);
        }

        private void AddParameter(DbCommand command, string name, object value, DbType dbType)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = ParameterName(name);
            parameter.DbType = dbType;
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }

        private string CreateParameterList(string baseName, int count)
            => string.Join(", ", Enumerable.Range(0, count).Select(index => Parameter(baseName + index)));

        private string Parameter(string name)
            => _dialect.ParameterPrefix + name;

        private string ParameterName(string name)
            => name;

        private async Task<int> ExecuteScalarIntAsync(DbCommand command)
        {
            var result = await command.ExecuteScalarAsync();
            return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result, CultureInfo.InvariantCulture);
        }

        private ExecutionHistoryEntry ReadEntry(DbDataReader reader)
        {
            return new ExecutionHistoryEntry
            {
                FireInstanceId = reader.GetString(reader.GetOrdinal("fire_instance_id")),
                SchedulerInstanceId = reader.GetString(reader.GetOrdinal("scheduler_instance_id")),
                SchedulerName = reader.GetString(reader.GetOrdinal("scheduler_name")),
                Job = reader.GetString(reader.GetOrdinal("job_name")),
                Trigger = reader.GetString(reader.GetOrdinal("trigger_name")),
                ScheduledFireTimeUtc = ReadNullableTimestamp(reader, "scheduled_fire_time_utc"),
                ActualFireTimeUtc = ReadRequiredTimestamp(reader, "actual_fire_time_utc"),
                Recovering = ReadBoolean(reader, "recovering"),
                Vetoed = ReadBoolean(reader, "vetoed"),
                FinishedTimeUtc = ReadNullableTimestamp(reader, "finished_time_utc"),
                ExceptionMessage = ReadNullableString(reader, "exception_message")
            };
        }

        private static string FormatTimestamp(DateTimeOffset? value)
            => value?.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

        private static DateTimeOffset ReadRequiredTimestamp(DbDataReader reader, string columnName)
            => DateTimeOffset.Parse(reader.GetString(reader.GetOrdinal(columnName)), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

        private static DateTimeOffset? ReadNullableTimestamp(DbDataReader reader, string columnName)
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal)
                ? (DateTimeOffset?)null
                : DateTimeOffset.Parse(reader.GetString(ordinal), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }

        private static string ReadNullableString(DbDataReader reader, string columnName)
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }

        private static bool ReadBoolean(DbDataReader reader, string columnName)
        {
            var ordinal = reader.GetOrdinal(columnName);
            return Convert.ToInt32(reader.GetValue(ordinal), CultureInfo.InvariantCulture) != 0;
        }
    }

    internal sealed class RelationalExecutionHistoryStoreDialect
    {
        public string ParameterPrefix { get; }
        public string ExecutionHistoryTableName { get; }
        public string JobStatsTableName { get; }
        public string ExecutionHistoryTableExistsSql { get; }
        public string JobStatsTableExistsSql { get; }
        public string CreateExecutionHistoryTableSql { get; }
        public string CreateJobStatsTableSql { get; }
        public bool RequiresUpperCaseTableNameParameter { get; }
        public bool IsOracle { get; }

        private RelationalExecutionHistoryStoreDialect(
            string parameterPrefix,
            string executionHistoryTableName,
            string jobStatsTableName,
            string executionHistoryTableExistsSql,
            string jobStatsTableExistsSql,
            string createExecutionHistoryTableSql,
            string createJobStatsTableSql,
            bool requiresUpperCaseTableNameParameter = false,
            bool isOracle = false)
        {
            ParameterPrefix = parameterPrefix;
            ExecutionHistoryTableName = executionHistoryTableName;
            JobStatsTableName = jobStatsTableName;
            ExecutionHistoryTableExistsSql = executionHistoryTableExistsSql;
            JobStatsTableExistsSql = jobStatsTableExistsSql;
            CreateExecutionHistoryTableSql = createExecutionHistoryTableSql;
            CreateJobStatsTableSql = createJobStatsTableSql;
            RequiresUpperCaseTableNameParameter = requiresUpperCaseTableNameParameter;
            IsOracle = isOracle;
        }

        public static RelationalExecutionHistoryStoreDialect Create(RelationalExecutionHistoryStoreSettings settings)
        {
            var executionHistoryTableName = $"{settings.TablePrefix}ExecutionHistoryStore";
            var jobStatsTableName = $"{settings.TablePrefix}JobStats";

            return settings.ProviderInvariantName switch
            {
                _ when IsMatch(settings.ProviderInvariantName, "sqlite") => new RelationalExecutionHistoryStoreDialect(
                    "@",
                    executionHistoryTableName,
                    jobStatsTableName,
                    "SELECT COUNT(1) FROM sqlite_master WHERE type = 'table' AND name = @TableName;",
                    "SELECT COUNT(1) FROM sqlite_master WHERE type = 'table' AND name = @TableName;",
                    $@"
CREATE TABLE {executionHistoryTableName} (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    fire_instance_id TEXT NOT NULL UNIQUE,
    scheduler_instance_id TEXT NOT NULL,
    scheduler_name TEXT NOT NULL,
    job_name TEXT NOT NULL,
    trigger_name TEXT NOT NULL,
    scheduled_fire_time_utc TEXT NULL,
    actual_fire_time_utc TEXT NOT NULL,
    recovering INTEGER NOT NULL,
    vetoed INTEGER NOT NULL,
    finished_time_utc TEXT NULL,
    exception_message TEXT NULL
);",
                    $@"
CREATE TABLE {jobStatsTableName} (
    id INTEGER NOT NULL PRIMARY KEY,
    total_jobs_executed INTEGER NOT NULL,
    total_jobs_failed INTEGER NOT NULL
);"),
                _ when IsMatch(settings.ProviderInvariantName, "npgsql", "postgres", "postgresql") => new RelationalExecutionHistoryStoreDialect(
                    "@",
                    executionHistoryTableName,
                    jobStatsTableName,
                    "SELECT COUNT(1) FROM information_schema.tables WHERE table_schema = current_schema() AND table_name = @TableName;",
                    "SELECT COUNT(1) FROM information_schema.tables WHERE table_schema = current_schema() AND table_name = @TableName;",
                    $@"
CREATE TABLE {executionHistoryTableName} (
    id BIGINT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    fire_instance_id VARCHAR(512) NOT NULL UNIQUE,
    scheduler_instance_id VARCHAR(512) NOT NULL,
    scheduler_name VARCHAR(512) NOT NULL,
    job_name VARCHAR(512) NOT NULL,
    trigger_name VARCHAR(512) NOT NULL,
    scheduled_fire_time_utc VARCHAR(64) NULL,
    actual_fire_time_utc VARCHAR(64) NOT NULL,
    recovering INTEGER NOT NULL,
    vetoed INTEGER NOT NULL,
    finished_time_utc VARCHAR(64) NULL,
    exception_message TEXT NULL
);",
                    $@"
CREATE TABLE {jobStatsTableName} (
    id INTEGER NOT NULL PRIMARY KEY,
    total_jobs_executed INTEGER NOT NULL,
    total_jobs_failed INTEGER NOT NULL
);"),
                _ when IsMatch(settings.ProviderInvariantName, "mysql") => new RelationalExecutionHistoryStoreDialect(
                    "@",
                    executionHistoryTableName,
                    jobStatsTableName,
                    "SELECT COUNT(1) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = @TableName;",
                    "SELECT COUNT(1) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = @TableName;",
                    $@"
CREATE TABLE {executionHistoryTableName} (
    id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    fire_instance_id VARCHAR(512) NOT NULL UNIQUE,
    scheduler_instance_id VARCHAR(512) NOT NULL,
    scheduler_name VARCHAR(512) NOT NULL,
    job_name VARCHAR(512) NOT NULL,
    trigger_name VARCHAR(512) NOT NULL,
    scheduled_fire_time_utc VARCHAR(64) NULL,
    actual_fire_time_utc VARCHAR(64) NOT NULL,
    recovering INT NOT NULL,
    vetoed INT NOT NULL,
    finished_time_utc VARCHAR(64) NULL,
    exception_message TEXT NULL
);",
                    $@"
CREATE TABLE {jobStatsTableName} (
    id INT NOT NULL PRIMARY KEY,
    total_jobs_executed INT NOT NULL,
    total_jobs_failed INT NOT NULL
);"),
                _ when IsMatch(settings.ProviderInvariantName, "sqlclient", "sqlserver") => new RelationalExecutionHistoryStoreDialect(
                    "@",
                    executionHistoryTableName,
                    jobStatsTableName,
                    "SELECT COUNT(1) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = SCHEMA_NAME() AND TABLE_NAME = @TableName;",
                    "SELECT COUNT(1) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = SCHEMA_NAME() AND TABLE_NAME = @TableName;",
                    $@"
CREATE TABLE {executionHistoryTableName} (
    id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    fire_instance_id NVARCHAR(512) NOT NULL UNIQUE,
    scheduler_instance_id NVARCHAR(512) NOT NULL,
    scheduler_name NVARCHAR(512) NOT NULL,
    job_name NVARCHAR(512) NOT NULL,
    trigger_name NVARCHAR(512) NOT NULL,
    scheduled_fire_time_utc NVARCHAR(64) NULL,
    actual_fire_time_utc NVARCHAR(64) NOT NULL,
    recovering INT NOT NULL,
    vetoed INT NOT NULL,
    finished_time_utc NVARCHAR(64) NULL,
    exception_message NVARCHAR(MAX) NULL
);",
                    $@"
CREATE TABLE {jobStatsTableName} (
    id INT NOT NULL PRIMARY KEY,
    total_jobs_executed INT NOT NULL,
    total_jobs_failed INT NOT NULL
);"),
                _ when IsMatch(settings.ProviderInvariantName, "oracle") => new RelationalExecutionHistoryStoreDialect(
                    ":",
                    executionHistoryTableName,
                    jobStatsTableName,
                    "SELECT COUNT(1) FROM user_tables WHERE table_name = :TableName",
                    "SELECT COUNT(1) FROM user_tables WHERE table_name = :TableName",
                    $@"
CREATE TABLE {executionHistoryTableName} (
    id NUMBER(19) GENERATED BY DEFAULT ON NULL AS IDENTITY PRIMARY KEY,
    fire_instance_id VARCHAR2(512 CHAR) NOT NULL UNIQUE,
    scheduler_instance_id VARCHAR2(512 CHAR) NOT NULL,
    scheduler_name VARCHAR2(512 CHAR) NOT NULL,
    job_name VARCHAR2(512 CHAR) NOT NULL,
    trigger_name VARCHAR2(512 CHAR) NOT NULL,
    scheduled_fire_time_utc VARCHAR2(64 CHAR) NULL,
    actual_fire_time_utc VARCHAR2(64 CHAR) NOT NULL,
    recovering NUMBER(10) NOT NULL,
    vetoed NUMBER(10) NOT NULL,
    finished_time_utc VARCHAR2(64 CHAR) NULL,
    exception_message CLOB NULL
);",
                    $@"
CREATE TABLE {jobStatsTableName} (
    id NUMBER(10) NOT NULL PRIMARY KEY,
    total_jobs_executed NUMBER(10) NOT NULL,
    total_jobs_failed NUMBER(10) NOT NULL
);",
                    requiresUpperCaseTableNameParameter: true,
                    isOracle: true),
                _ => throw new NotSupportedException($"Unsupported ADO.NET provider '{settings.ProviderInvariantName}'.")
            };
        }

        private static bool IsMatch(string providerInvariantName, params string[] tokens)
        {
            var normalized = providerInvariantName?.Trim().ToLowerInvariant() ?? string.Empty;
            return tokens.Any(token => normalized.Contains(token));
        }
    }
}
