using Microsoft.EntityFrameworkCore;
using NationStatesAPIBot.Entities;
using NationStatesAPIBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NationStatesAPIBot.Manager
{
    public class NationManager
    {
        private static AppSettings _config;
        private static ReaderWriterLockSlim nationLock = new ReaderWriterLockSlim();
        private static HashSet<string> nationCache = new HashSet<string>();

        public static async Task InitializeAsync(AppSettings config)
        {
            _config = config;
            await LoadCacheAsync();
        }

        public static async Task LoadCacheAsync()
        {
            nationLock.EnterReadLock();
            try
            {
                using (var dbContext = new BotDbContext(_config))
                {
                    await foreach (var nation in dbContext.Nations.AsQueryable().ToAsyncEnumerable())
                    {
                        nationCache.Add(nation.Name);
                    }
                }
            }
            finally
            {
                nationLock.ExitReadLock();
            }
        }

        public static async Task<Nation> GetNationAsync(string nationName)
        {
            using (var dbContext = new BotDbContext(_config))
            {
                return await dbContext.Nations.AsQueryable().FirstOrDefaultAsync(n => n.Name == nationName);
            }
        }

        public static List<Nation> GetNationsByStatusName(string name)
        {
            using (var dbContext = new BotDbContext(_config))
            {
                return dbContext.Nations.AsQueryable().Where(n => n.Status.Name == name).OrderByDescending(n => n.StatusTime).ToList();
            }
        }

        public static async Task<Nation> GetNationByStatusNameAsync(string name)
        {
            using (var dbContext = new BotDbContext(_config))
            {
                return await dbContext.Nations.AsQueryable().Where(n => n.Status.Name == name).OrderByDescending(n => n.StatusTime).FirstOrDefaultAsync();
            }
        }

        public static int GetNationCountByStatusName(string name)
        {
            using (var dbContext = new BotDbContext(_config))
            {
                return dbContext.Nations.Count(n => n.Status.Name == name);
            }
        }

        public static bool IsNationInDb(string name)
        {
            nationLock.EnterReadLock();
            try
            {
                return nationCache.Contains(name);
            }
            finally
            {
                nationLock.ExitReadLock();
            }
        }

        public static async Task<bool> IsNationPendingSkippedSendOrFailedAsync(string name)
        {
            using (var dbContext = new BotDbContext(_config))
            {
                var nation = await dbContext.Nations.Include(n => n.Status).FirstOrDefaultAsync
                    (
                    n => n.Name == name
                    && (n.Status.Name == "pending"
                    || n.Status.Name == "skipped"
                    || n.Status.Name == "send"
                    || n.Status.Name == "failed")
                    );
                return nation != null;
            }
        }

        public static async Task SetNationStatusToAsync(Nation nation, string statusName)
        {
            using (var dbContext = new BotDbContext(_config))
            {
                var current = nation.Status;
                var status = await dbContext.NationStatuses.AsQueryable().FirstOrDefaultAsync(n => n.Name == statusName);
                if (status == null)
                {
                    status = new NationStatus() { Name = statusName };
                    await dbContext.NationStatuses.AddAsync(status);
                    await dbContext.SaveChangesAsync();
                }
                nation.Status = status;
                nation.StatusId = status.Id;
                nation.StatusTime = DateTime.UtcNow;
                dbContext.Nations.Update(nation);
                await dbContext.SaveChangesAsync();
            }
        }

        public static async Task<int> AddUnknownNationsAsPendingAsync(List<string> newNations)
        {
            nationLock.EnterWriteLock();
            try
            {
                int counter = 0;
                using (var context = new BotDbContext(_config))
                {
                    var status = await context.NationStatuses.AsQueryable().FirstOrDefaultAsync(n => n.Name == "pending");
                    if (status == null)
                    {
                        status = new NationStatus() { Name = "pending" };
                        await context.NationStatuses.AddAsync(status);
                        await context.SaveChangesAsync();
                    }
                    foreach (string nationName in newNations)
                    {
                        if (!nationCache.Contains(nationName))
                        {
                            var time = DateTime.UtcNow;
                            await context.Nations.AddAsync(new Nation() { Name = nationName, StatusTime = time, Status = status, StatusId = status.Id });
                            nationCache.Add(nationName);
                            counter++;
                        }
                    }
                    await context.SaveChangesAsync();
                }
                return counter;
            }
            finally
            {
                nationLock.ExitWriteLock();
            }
        }
    }
}