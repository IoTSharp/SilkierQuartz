using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Quartz.Plugins.RecentHistory.Impl
{
    [Serializable]
    public class SqlServerExecutionHistoryStore : IExecutionHistoryStore
    {
        public string SchedulerName { get; set; }


        DateTime _nextPurgeTime = DateTime.UtcNow;
        int _updatesFromLastPurge;

        int _totalJobsExecuted = 0, _totalJobsFailed = 0;
        private const string _prefix = "tb_quartz_";
        private readonly IDbConnection _dbConnection;
        public Task<ExecutionHistoryEntry> Get(string fireInstanceId)
        {
            var sql = $"SELECT * FROM {_prefix}ExecutionHistoryStore WHERE fire_instance_id = @FireInstanceId";
            return  _dbConnection.QuerySingleOrDefaultAsync<ExecutionHistoryEntry>(sql, new { FireInstanceId = fireInstanceId });
        }

        public async Task Purge()
        {
            var ids = new HashSet<string>((await FilterLastOfEveryTrigger(10)).Select(x => x.FireInstanceId));
            var sql = $"DELETE FROM  {_prefix}ExecutionHistoryStore WHERE id NOT IN @Ids";
            await _dbConnection.ExecuteAsync(sql, new { Ids = ids });
        }

        public async Task Save(ExecutionHistoryEntry entry)
        {
            _updatesFromLastPurge++;

            if (_updatesFromLastPurge >= 10 || _nextPurgeTime < DateTime.UtcNow)
            {
                _nextPurgeTime = DateTime.UtcNow.AddMinutes(1);
                _updatesFromLastPurge = 0;
                await Purge();
            }
            var sql = $@"
            INSERT INTO {_prefix}ExecutionHistoryStore (
                fire_instance_id,
                scheduler_instance_id,
                scheduler_name,
                job,
                trigger,
                scheduled_fire_time_utc,
                actual_fire_time_utc,
                recovering,
                vetoed,
                finished_time_utc,
                exception_message
            ) VALUES (
                @FireInstanceId,
                @SchedulerInstanceId,
                @SchedulerName,
                @Job,
                @Trigger,
                @ScheduledFireTimeUtc,
                @ActualFireTimeUtc,
                @Recovering,
                @Vetoed,
                @FinishedTimeUtc,
                @ExceptionMessage
            )";
             await _dbConnection.ExecuteAsync(sql, entry);
        }

        public  Task<IEnumerable<ExecutionHistoryEntry>> FilterLastOfEveryJob(int limitPerJob)
        {
            var sql = $@"
SELECT *
FROM (
    SELECT
        *,
        ROW_NUMBER() OVER (
            PARTITION BY job
            ORDER BY actual_fire_time_utc DESC
        ) AS rn
    FROM {_prefix}ExecutionHistoryStore
    WHERE scheduler_name = @SchedulerName
) t
WHERE t.rn <= @LimitPerJob
ORDER BY job, actual_fire_time_utc ASC;
";

            var result =  _dbConnection.QueryAsync<ExecutionHistoryEntry>(
                sql, new { SchedulerName = SchedulerName, LimitPerJob = limitPerJob });
            return result;
        }

        public Task<IEnumerable<ExecutionHistoryEntry>> FilterLastOfEveryTrigger(int limitPerTrigger)
        {

            var sql = $@"
SELECT *
FROM (
    SELECT
        *,
        ROW_NUMBER() OVER (
            PARTITION BY trigger
            ORDER BY actual_fire_time_utc DESC
        ) AS rn
    FROM {_prefix}ExecutionHistoryStore
    WHERE scheduler_name = @SchedulerName
) t
WHERE t.rn <= @LimitPerTrigger
ORDER BY trigger, actual_fire_time_utc ASC;
";

            var result =  _dbConnection.QueryAsync<ExecutionHistoryEntry>(
                sql, new {  SchedulerName, LimitPerTrigger = limitPerTrigger });
            return result;
        }

        public Task<IEnumerable<ExecutionHistoryEntry>> FilterLast(int limit)
        {

            var sql = $@"
SELECT *
FROM (
    SELECT *
    FROM {_prefix}ExecutionHistoryStore
    WHERE scheduler_name = @SchedulerName
    ORDER BY actual_fire_time_utc DESC
    LIMIT @Limit
) t
ORDER BY actual_fire_time_utc ASC;
";

            var result =  _dbConnection.QueryAsync<ExecutionHistoryEntry>(
                sql, new { SchedulerName, Limit = limit });
            return result;
        }

        private const int StatsId = 1; // 只有一行，ID恒为1
       
        public async Task<int> GetTotalJobsExecuted()
        {
            var sql = $@"SELECT total_jobs_executed AS TotalJobsExecuted, total_jobs_failed AS TotalJobsFailed 
                    FROM  {_prefix}JobStats  WHERE id = @Id";
              var js= await  _dbConnection.QuerySingleAsync<JobStats>(sql, new { Id = StatsId });
            return js.TotalJobsExecuted;
        }
        public async Task<int> GetTotalJobsFailed()
        {
            var sql = $@"SELECT total_jobs_executed AS TotalJobsExecuted, total_jobs_failed AS TotalJobsFailed 
                    FROM  {_prefix}JobStats WHERE id = @Id";
            var js = await _dbConnection.QuerySingleAsync<JobStats>(sql, new { Id = StatsId });
            return js.TotalJobsFailed;
        }

        public async Task IncrementTotalJobsExecuted()
        {
            var sql = $@"UPDATE {_prefix}JobStats  SET total_jobs_executed = total_jobs_executed + 1 WHERE id = @Id";
            await _dbConnection.ExecuteAsync(sql, new { Id = StatsId });
        }

        public async Task IncrementTotalJobsFailed()
        {
            var sql = $@"UPDATE {_prefix}JobStats  SET total_jobs_failed = total_jobs_failed + 1 WHERE id = @Id";
            await _dbConnection.ExecuteAsync(sql, new { Id = StatsId });
        }
    }
}
