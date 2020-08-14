using Microsoft.EntityFrameworkCore;
using NationStatesAPIBot.Entities;
using NationStatesAPIBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NationStatesAPIBot.Manager
{
    public class NationManager
    {
        private static AppSettings _config;

        public static void Initialize(AppSettings config)
        {
            _config = config;
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

        public static async Task<int> AddUnknownNationsAsPendingAsync(List<string> newNations, bool sourceDumps)
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
                    if (await context.Nations.AsQueryable().FirstOrDefaultAsync(n => n.Name == nationName) == null)
                    {
                        var time = sourceDumps ? TimeZoneInfo.ConvertTimeToUtc(DumpDataService.LastDumpUpdateTimeUtc, TimeZoneInfo.Local) : DateTime.UtcNow;
                        await context.Nations.AddAsync(new Nation() { Name = nationName, StatusTime = time, Status = status, StatusId = status.Id });
                        await context.SaveChangesAsync();
                        counter++;
                    }
                }
            }
            return counter;
        }
    }
}