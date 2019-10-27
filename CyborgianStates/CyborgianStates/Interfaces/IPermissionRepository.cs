using Discord.WebSocket;
using CyborgianStates.Types;
using System.Collections.Generic;
using System.Threading.Tasks;
using CyborgianStates.Models;

namespace CyborgianStates.Interfaces
{
    public interface IPermissionRepository
    {
        Task AddPermissionAsync(string discordUserId, Role role);
        Task RevokePermissionAsync(string discordUserId, Permission permission);
        Task AddPermissionToRoleAsync(Role role, Permission permission);
        Task RevokePermissionFromRoleAsync(Role role, Permission permission);
        Task AddRoleAsync(string roleDescription);
        Task RemoveRoleAsync(Role role);
        Task<IEnumerable<Permission>> GetAllPermissionsToAUserAsync(string discordUserId);
        Task<IEnumerable<Permission>> GetRolePermissionsAsync(long roleId);
        Task<IEnumerable<Role>> GetRolesAsync(string discordUserId);
        Task<IEnumerable<Permission>> GetUserPermissionsAsync(string discordUserId);
        Task<bool> IsAllowedAsync(PermissionType permissionType, SocketUser user);
        Task<bool> IsBotAdminAsync(SocketUser user);
    }
}
