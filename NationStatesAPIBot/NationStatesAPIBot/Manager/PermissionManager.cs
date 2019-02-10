using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using NationStatesAPIBot.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NationStatesAPIBot.Managers
{
    public static class PermissionManager
    {
        public static async Task<bool> IsAllowed(PermissionType permissionType, SocketUser user)
        {
            if (ActionManager.BotAdminDiscordUserId == user.Id.ToString())
            {
                return true;
            }
            else
            {
                using (var context = new BotDbContext())
                {
                    var returnedUser = await context.Users.Where(u => u.DiscordUserId == user.Id.ToString()).FirstOrDefaultAsync();
                    if (returnedUser != null && (returnedUser.UserPermissions.Count > 0 || returnedUser.Roles.Count > 0))
                    {
                        return (returnedUser.UserPermissions.Exists(p => p.Permission.Id == (long)permissionType)
                            || returnedUser.Roles.Exists(r => r.Role.RolePermissions.Exists(rp => rp.Permission.Id == (long)permissionType)));
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }
    }
}
