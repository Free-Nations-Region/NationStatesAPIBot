using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NationStatesAPIBot.Managers
{
    public static class PermissionManager
    {
        public static async Task<bool> IsAllowed(long permissionId, SocketUser user)
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
                    if (returnedUser != null)
                    {
                        return returnedUser.UserPermissions.Exists(p => p.Permission.Id == permissionId)
                            || returnedUser.Roles.Exists(r => r.Role.RolePermissions.Exists(rp => rp.Permission.Id == permissionId));
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
