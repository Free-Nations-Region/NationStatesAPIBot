using Microsoft.EntityFrameworkCore;
using NationStatesAPIBot.Entities;
using NationStatesAPIBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NationStatesAPIBot.Manager
{
    public class NationManager
    {
        public static List<Nation> GetNationsByStatusName(string name)
        {
            using (var dbContext = new BotDbContext())
            {
                return dbContext.Nations.Where(n => n.Status.Name == name).OrderByDescending(n => n.StatusTime).ToList();
            }
        }

        public static async Task SetNationStatusToAsync(Nation nation, string statusName)
        {
            using (var dbContext = new BotDbContext())
            {
                var current = nation.Status;
                var status = await dbContext.NationStatuses.FirstOrDefaultAsync(n => n.Name == statusName);
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
            int counter = 0;
            using (var context = new BotDbContext())
            {
                var status = await context.NationStatuses.FirstOrDefaultAsync(n => n.Name == "pending");
                if (status == null)
                {
                    status = new NationStatus() { Name = "pending" };
                    await context.NationStatuses.AddAsync(status);
                }
                foreach (string nationName in newNations)
                {
                    if(!await context.Nations.AnyAsync(n => n.Name == nationName))
                    {
                        await context.Nations.AddAsync(new Nation() { Name = nationName, StatusTime = DateTime.UtcNow, Status = status, StatusId = status.Id });
                        counter++;
                    }
                }                
            }
            return counter;
        }
    }
}
