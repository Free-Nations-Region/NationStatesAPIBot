using NationStatesAPIBot.Entities;
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
            throw new NotImplementedException();
        }
    }
}
