using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NationStatesAPIBot.Interfaces
{
    public interface IPermissionManager
    {
        Task<bool> IsBotAdminAsync(string userId);
    }
}
