using Microsoft.EntityFrameworkCore;
using Quartz.Plugins.RecentHistory;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Quartz.Plugins.RecentHistory.EFCoreNpgsql
{
    /// <summary>
    /// Stores Quartz execution history through EF Core.
    /// </summary>
    [Serializable]
    public sealed class EfCoreExecutionHistoryStore : IExecutionHistoryStore
    {
        private const int StatsId = 1;
        private static readonly string TimestampFormat = "O";

        private readonly IDbContextFactory<ExecutionHistoryDbContext> _dbContextFactory;
        private readonly SemaphoreSlim _initializationLock = new SemaphoreSlim(1, 1);

        private volatile bool _initialized;
        private DateTime _nextPurgeTime = DateTime.UtcNow;
        private int _updatesFromLastPurge;

        public EfCoreExecutionHistoryStore(IDbContextFactory<ExecutionHistoryDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
        }

        public string SchedulerName { get; set; } = string.Empty;

        /// <inheritdoc />
        public async Task<ExecutionHistoryEntry> Get(string fireInstanceId)
        {
            if (string.IsNullOrWhiteSpace(fireInstanceId))
            {
                throw new ArgumentException("A fire instance id is required.", nameof(fireInstanceId));
            }

            await EnsureInitializedAsync().ConfigureAwait(false);

            await using var dbContext = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
            var entity = await dbContext.ExecutionHistory
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.FireInstanceId == fireInstanceId)
                .ConfigureAwait(false);

            return entity == null ? null : ToEntry(entity);
        }

        /// <inheritdoc />
        public async Task Save(ExecutionHistoryEntry entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            await EnsureInitializedAsync().ConfigureAwait(false);

            _updatesFromLastPurge++;
            if (_updatesFromLastPurge >= 10 || _nextPurgeTime < DateTime.UtcNow)
            {
                _nextPurgeTime = DateTime.UtcNow.AddMinutes(1);
                _updatesFromLastPurge = 0;
                await Purge().ConfigureAwait(false);
            }

            await using var dbContext = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
            var entity = await dbContext.ExecutionHistory
                .SingleOrDefaultAsync(x => x.FireInstanceId == entry.FireInstanceId)
                .ConfigureAwait(false);

            if (entity == null)
            {
                entity = new ExecutionHistoryDbContext.ExecutionHistoryRow();
                UpdateEntity(entity, entry);
                dbContext.ExecutionHistory.Add(entity);
            }
            else
            {
                UpdateEntity(entity, entry);
            }

            await dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task Purge()
        {
            await EnsureInitializedAsync().ConfigureAwait(false);

            await using var dbContext = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
            var keepIds = (await dbContext.ExecutionHistory
                .AsNoTracking()
                .Where(x => x.SchedulerName == SchedulerName)
                .OrderBy(x => x.TriggerName)
                .ThenByDescending(x => x.ActualFireTimeUtc)
                .ThenByDescending(x => x.Id)
                .Select(x => new { x.TriggerName, x.FireInstanceId })
                .ToListAsync()
                .ConfigureAwait(false))
                .GroupBy(x => x.TriggerName, StringComparer.Ordinal)
                .SelectMany(group => group.Take(10))
                .Select(x => x.FireInstanceId)
                .ToArray();

            var query = dbContext.ExecutionHistory.Where(x => x.SchedulerName == SchedulerName);
            if (keepIds.Length > 0)
            {
                query = query.Where(x => !keepIds.Contains(x.FireInstanceId));
            }

            await query.ExecuteDeleteAsync().ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task<IEnumerable<ExecutionHistoryEntry>> FilterLastOfEveryJob(int limitPerJob)
        {
            if (limitPerJob <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(limitPerJob));
            }

            return FilterPerGroupAsync(limitPerJob, static x => x.JobName);
        }

        /// <inheritdoc />
        public Task<IEnumerable<ExecutionHistoryEntry>> FilterLastOfEveryTrigger(int limitPerTrigger)
        {
            if (limitPerTrigger <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(limitPerTrigger));
            }

            return FilterPerGroupAsync(limitPerTrigger, static x => x.TriggerName);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<ExecutionHistoryEntry>> FilterLast(int limit)
        {
            if (limit <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(limit));
            }

            await EnsureInitializedAsync().ConfigureAwait(false);

            await using var dbContext = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
            var entries = await dbContext.ExecutionHistory
                .AsNoTracking()
                .Where(x => x.SchedulerName == SchedulerName)
                .OrderByDescending(x => x.ActualFireTimeUtc)
                .ThenByDescending(x => x.Id)
                .Take(limit)
                .ToListAsync()
                .ConfigureAwait(false);

            return entries
                .OrderBy(x => x.ActualFireTimeUtc, StringComparer.Ordinal)
                .Select(ToEntry)
                .ToArray();
        }

        /// <inheritdoc />
        public async Task<int> GetTotalJobsExecuted()
        {
            await EnsureInitializedAsync().ConfigureAwait(false);

            await using var dbContext = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
            var stats = await dbContext.JobStats
                .AsNoTracking()
                .SingleAsync(x => x.Id == StatsId)
                .ConfigureAwait(false);

            return stats.TotalJobsExecuted;
        }

        /// <inheritdoc />
        public async Task<int> GetTotalJobsFailed()
        {
            await EnsureInitializedAsync().ConfigureAwait(false);

            await using var dbContext = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
            var stats = await dbContext.JobStats
                .AsNoTracking()
                .SingleAsync(x => x.Id == StatsId)
                .ConfigureAwait(false);

            return stats.TotalJobsFailed;
        }

        /// <inheritdoc />
        public async Task IncrementTotalJobsExecuted()
        {
            await EnsureInitializedAsync().ConfigureAwait(false);

            await using var dbContext = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
            await dbContext.JobStats
                .Where(x => x.Id == StatsId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.TotalJobsExecuted, x => x.TotalJobsExecuted + 1))
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task IncrementTotalJobsFailed()
        {
            await EnsureInitializedAsync().ConfigureAwait(false);

            await using var dbContext = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
            await dbContext.JobStats
                .Where(x => x.Id == StatsId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.TotalJobsFailed, x => x.TotalJobsFailed + 1))
                .ConfigureAwait(false);
        }

        private async Task<IEnumerable<ExecutionHistoryEntry>> FilterPerGroupAsync(int limit, Func<ExecutionHistoryDbContext.ExecutionHistoryRow, string> groupSelector)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);

            await using var dbContext = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
            var rows = await dbContext.ExecutionHistory
                .AsNoTracking()
                .Where(x => x.SchedulerName == SchedulerName)
                .OrderByDescending(x => x.ActualFireTimeUtc)
                .ThenByDescending(x => x.Id)
                .ToListAsync()
                .ConfigureAwait(false);

            return rows
                .GroupBy(groupSelector, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .SelectMany(group => group.Take(limit).OrderBy(x => x.ActualFireTimeUtc, StringComparer.Ordinal))
                .Select(ToEntry)
                .ToArray();
        }

        private async Task EnsureInitializedAsync()
        {
            if (_initialized)
            {
                return;
            }

            await _initializationLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_initialized)
                {
                    return;
                }

                await using var dbContext = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
                await dbContext.Database.EnsureCreatedAsync().ConfigureAwait(false);

                var hasStats = await dbContext.JobStats
                    .AsNoTracking()
                    .AnyAsync(x => x.Id == StatsId)
                    .ConfigureAwait(false);

                if (!hasStats)
                {
                    dbContext.JobStats.Add(new ExecutionHistoryDbContext.JobStatsRow
                    {
                        Id = StatsId,
                        TotalJobsExecuted = 0,
                        TotalJobsFailed = 0
                    });

                    await dbContext.SaveChangesAsync().ConfigureAwait(false);
                }

                _initialized = true;
            }
            finally
            {
                _initializationLock.Release();
            }
        }

        private static ExecutionHistoryEntry ToEntry(ExecutionHistoryDbContext.ExecutionHistoryRow entity)
        {
            return new ExecutionHistoryEntry
            {
                FireInstanceId = entity.FireInstanceId,
                SchedulerInstanceId = entity.SchedulerInstanceId,
                SchedulerName = entity.SchedulerName,
                Job = entity.JobName,
                Trigger = entity.TriggerName,
                ScheduledFireTimeUtc = ParseNullableTimestamp(entity.ScheduledFireTimeUtc),
                ActualFireTimeUtc = ParseRequiredTimestamp(entity.ActualFireTimeUtc),
                Recovering = entity.Recovering,
                Vetoed = entity.Vetoed,
                FinishedTimeUtc = ParseNullableTimestamp(entity.FinishedTimeUtc),
                ExceptionMessage = entity.ExceptionMessage
            };
        }

        private static void UpdateEntity(ExecutionHistoryDbContext.ExecutionHistoryRow entity, ExecutionHistoryEntry entry)
        {
            entity.FireInstanceId = entry.FireInstanceId;
            entity.SchedulerInstanceId = entry.SchedulerInstanceId;
            entity.SchedulerName = entry.SchedulerName;
            entity.JobName = entry.Job;
            entity.TriggerName = entry.Trigger;
            entity.ScheduledFireTimeUtc = FormatTimestamp(entry.ScheduledFireTimeUtc);
            entity.ActualFireTimeUtc = FormatTimestamp(entry.ActualFireTimeUtc);
            entity.Recovering = entry.Recovering;
            entity.Vetoed = entry.Vetoed;
            entity.FinishedTimeUtc = FormatTimestamp(entry.FinishedTimeUtc);
            entity.ExceptionMessage = entry.ExceptionMessage;
        }

        private static string FormatTimestamp(DateTimeOffset value)
        {
            return value.ToUniversalTime().ToString(TimestampFormat, CultureInfo.InvariantCulture);
        }

        private static string FormatTimestamp(DateTimeOffset? value)
        {
            return value?.ToUniversalTime().ToString(TimestampFormat, CultureInfo.InvariantCulture);
        }

        private static DateTimeOffset ParseRequiredTimestamp(string value)
        {
            return DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }

        private static DateTimeOffset? ParseNullableTimestamp(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? (DateTimeOffset?)null
                : DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }
    }
}
