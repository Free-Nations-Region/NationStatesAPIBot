using CyborgianStates.Interfaces;
using CyborgianStates.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace CyborgianStates.Repositories
{
    public class NationRepository : INationRepository
    {
        IMongoCollection<Nation> nations;
        AppSettings _config;
        public NationRepository(IMongoDatabase database, IOptions<AppSettings> config)
        {
            if (database == null)
            {
                throw new ArgumentNullException(nameof(database));
            }
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            nations = database.GetCollection<Nation>("nation");
            _config = config.Value;
        }
        public async Task<int> BulkAddNationsToPending(List<string> newNations)
        {
            if (newNations == null)
            {
                throw new ArgumentNullException(nameof(newNations));
            }
            int counter = 0;
            foreach (string nationName in newNations)
            {
                if (await GetNationAsync(nationName) != null)
                {
                    var nation = new Nation()
                    {
                        Name = nationName,
                        Status = new List<Status>() { new Status() { Name = "pending", Active = true, CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow } }
                    };
                    await nations.InsertOneAsync(nation);
                }
            }
            return counter;
        }

        public async Task<Nation> GetNationAsync(string nationName)
        {
            var res = await nations.FindAsync(n => n.Name == nationName);
            var nation = await res.FirstOrDefaultAsync();
            return nation;
        }
        public async Task<int> GetNationCountByStatusNameAsync(string statusName)
        {
            return await GetNationCountByStatusNameAsync(statusName, true);
        }

        public async Task<int> GetNationCountByStatusNameAsync(string statusName, bool excludeInactive)
        {
            var list = await GetNationsByStatusNameAsync(statusName);
            return list.Count;
        }

        public async Task<List<Nation>> GetNationsByStatusNameAsync(string statusName)
        {
            return await GetNationsByStatusNameAsync(statusName, true);
        }

        public async Task<List<Nation>> GetNationsByStatusNameAsync(string statusName, bool excludeInactive)
        {
            var res = await nations.FindAsync(n => n.Status.Any(s => s.Name == statusName && (excludeInactive ? s.Active : true)));
            return await res.ToListAsync();
        }

        public async Task SetNationStatusAsync(Nation nation, string statusName, bool active)
        {
            if (nation == null)
            {
                throw new ArgumentNullException(nameof(nation));
            }
            if (nation.Status.Any(s => s.Name == statusName))
            {
                var status = nation.Status.FirstOrDefault(s => s.Name == statusName);
                status.Active = active;
                status.UpdateAt = DateTime.UtcNow;
            }
            else
            {
                nation.Status.Add(new Status() { Name = statusName, Active = active, CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow });
            }
            var filter = Builders<Nation>.Filter.Eq(n => n.Id, nation.Id);
            await nations.ReplaceOneAsync(filter, nation);
        }
    }
}
