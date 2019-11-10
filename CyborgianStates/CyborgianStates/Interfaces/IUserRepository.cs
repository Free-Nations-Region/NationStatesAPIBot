using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CyborgianStates.Interfaces
{
    public interface IUserRepository
    {
        Task<bool> IsUserInDbAsync(ulong userId);
        Task AddUserToDbAsync(ulong userId);
        Task RemoveUserFromDbAsync(ulong userId);
        Task<bool> IsAllowedAsync(string permissionType, ulong userId);
        Task<bool> IsBotAdminAsync(ulong userId);
    }
}
