using CyborgianStates.Interfaces;
using CyborgianStates.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CyborgianStates.Repositories
{
    public class NationRepository : INationRepository
    {
        public Task<int> BulkAddNationsToPending(List<string> newNations, bool sourceDump)
        {
            throw new NotImplementedException();
        }

        public Task<Nation> GetNationAsync(string nationName)
        {
            throw new NotImplementedException();
        }

        public Task<int> GetNationCountByStatusName(string statusName)
        {
            throw new NotImplementedException();
        }

        public Task<List<Nation>> GetNationsByStatusNameAsync(string statusName)
        {
            throw new NotImplementedException();
        }

        public Task SetNationStatusAsync(Nation nation, string statusName, bool active)
        {
            throw new NotImplementedException();
        }
    }
}
