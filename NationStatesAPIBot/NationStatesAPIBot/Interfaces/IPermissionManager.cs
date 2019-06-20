using Discord.WebSocket;
using NationStatesAPIBot.Entities;
using NationStatesAPIBot.Types;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NationStatesAPIBot.Interfaces
{
    public interface IPermissionManager
    {
        Task AddPermissionAsync(string discordUserId, Role role, BotDbContext dbContext);
        Task RevokePermissionAsync(string discordUserId, Permission permission, BotDbContext dbContext);
        Task AddPermissionToRoleAsync(Role role, Permission permission, BotDbContext dbContext);
        Task RevokePermissionFromRoleAsync(Role role, Permission permission, BotDbContext dbContext);
        Task AddRoleAsync(string roleDescription, BotDbContext dbContext);
        Task RemoveRoleAsync(Role role, BotDbContext dbContext);
        Task<IEnumerable<Permission>> GetAllPermissionsToAUserAsync(string discordUserId, BotDbContext dbContext);
        Task<IEnumerable<Permission>> GetRolePermissionsAsync(long roleId, BotDbContext dbContext);
        Task<IEnumerable<Role>> GetRolesAsync(string discordUserId, BotDbContext dbContext);
        Task<IEnumerable<Permission>> GetUserPermissionsAsync(string discordUserId, BotDbContext dbContext);
        Task<bool> IsAllowedAsync(PermissionType permissionType, SocketUser user);
        Task<bool> IsBotAdminAsync(SocketUser user);
    }
}
