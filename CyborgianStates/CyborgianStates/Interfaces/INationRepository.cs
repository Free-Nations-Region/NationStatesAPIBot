using CyborgianStates.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CyborgianStates.Interfaces
{
    public interface INationRepository
    {
        Task<Nation> GetNationAsync(string nationName);
        Task<List<Nation>> GetNationsByStatusNameAsync(string statusName);
        Task<List<Nation>> GetNationsByStatusNameAsync(string statusName, bool excludeInactive);
        Task<int> GetNationCountByStatusNameAsync(string statusName);
        Task<int> GetNationCountByStatusNameAsync(string statusName, bool excludeInactive);
        Task SetNationStatusAsync(Nation nation, string statusName, bool active);
        Task<int> BulkAddNationsToPending(List<string> newNations);
    }
}
