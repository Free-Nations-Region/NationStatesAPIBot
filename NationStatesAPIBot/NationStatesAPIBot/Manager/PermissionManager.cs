using Discord.WebSocket;
using NationStatesAPIBot.Entities;
using NationStatesAPIBot.Types;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NationStatesAPIBot.Managers
{
    public static class PermissionManager
    {
        public static bool IsAllowed(PermissionType permissionType, SocketUser user)
        {
            if (ActionManager.IsBotAdmin(user))
            {
                return true;
            }
            else
            {
                using (var dbContext = new BotDbContext())
                {
                    var perms = GetAllPermissionsToAUser(user.Id.ToString(), dbContext);
                    return perms.Select(p => p.Id).Contains((long)permissionType);
                }
            }
        }

        public static IQueryable<Permission> GetUserPermissions(string discordUserId, BotDbContext dbContext)
        {
            var perms = dbContext.Users.Where(u => u.DiscordUserId == discordUserId).SelectMany(u => u.UserPermissions).Select(p => p.Permission);
            return perms;
        }

        public static IQueryable<Role> GetRoles(string discordUserId, BotDbContext dbContext)
        {
            return dbContext.Users.Where(u => u.DiscordUserId == discordUserId).SelectMany(u => u.Roles).Select(r => r.Role);
        }

        public static IQueryable<Permission> GetRolePermissions(long roleId, BotDbContext dbContext)
        {
            return dbContext.Roles.Where(r => r.Id == roleId).SelectMany(r => r.RolePermissions).Select(p => p.Permission);
        }

        public static List<Permission> GetAllPermissionsToAUser(string discordUserId, BotDbContext dbContext)
        {
            var userPerms = GetUserPermissions(discordUserId, dbContext).ToList();
            var roles = GetRoles(discordUserId, dbContext).ToList();
            var result = userPerms;
            foreach(Role role in roles)
            {
                result.AddRange(GetRolePermissions(role.Id, dbContext).Except(result));
            }
            return result;
        }
    }
}
