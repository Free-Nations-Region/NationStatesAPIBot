using CyborgianStates.Interfaces;
using CyborgianStates.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace CyborgianStates.Repositories
{
    public class NationRepository : INationRepository
    {
        IMongoCollection<Nation> nations;
        AppSettings _config;
        ILogger<NationRepository> _logger;

        public NationRepository(IMongoDatabase database, IOptions<AppSettings> config, ILogger<NationRepository> logger)
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
            _logger = logger;
        }
        public async Task<int> BulkAddNationsToPendingAsync(List<string> newNations)
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

        public async Task SwitchNationStatusAsync(Nation nation, string fromStatusName, string toStatusName, EventId eventId)
        {
            try
            {
                if (nation == null)
                {
                    throw new ArgumentNullException(nameof(nation));
                }
                if (nation.Status.Any(s => s.Name == fromStatusName && s.Active) && nation.Status.Any(s => s.Name == toStatusName && !s.Active))
                {
                    // switch active status to inactive
                    await SetNationStatusAsync(nation, fromStatusName, false);
                    // switch or add status to active
                    await SetNationStatusAsync(nation, toStatusName, true);
                }
                else
                {
                    _logger.LogWarning(eventId, LogMessageBuilder.Build(eventId, $"The nation id:{nation.Id} wasn't in the expected state. Expected active status: {fromStatusName} inactive status: {toStatusName}"));
                }
            }
            finally
            {
                LogEventIdProvider.ReleaseEventId(eventId);
            }
        }
    }
}
