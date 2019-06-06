using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NationStatesAPIBot.Entities;
using NationStatesAPIBot.Interfaces;
using NationStatesAPIBot.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NationStatesAPIBot.Managers
{
    public class PermissionManager : IPermissionManager
    {
        private readonly AppSettings _config;
        private readonly ILogger<PermissionManager> _logger;
        public PermissionManager(ILogger<PermissionManager> logger, IOptions<AppSettings> config)
        {
            _config = config.Value;
            _logger = logger;
        }

        public Task AddPermissionAsync(string discordUserId, Role role, BotDbContext dbContext)
        {
            throw new NotImplementedException();
        }

        public Task AddRoleAsync(string roleDescription, BotDbContext dbContext)
        {
            throw new NotImplementedException();
        }

        public Task RemoveRoleAsync(Role role, BotDbContext dbContext)
        {
            throw new NotImplementedException();
        }

        public Task RevokePermissionAsync(string discordUserId, Permission permission, BotDbContext dbContext)
        {
            throw new NotImplementedException();
        }

        public Task AddPermissionToRoleAsync(Role role, Permission permission, BotDbContext dbContext)
        {
            throw new NotImplementedException();
        }

        public Task RevokePermissionFromRoleAsync(Role role, Permission permission, BotDbContext dbContext)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<Permission>> GetAllPermissionsToAUserAsync(string discordUserId, BotDbContext dbContext)
        {
            var userPerms = await GetUserPermissionsAsync(discordUserId, dbContext);
            var roles = await GetRolesAsync(discordUserId, dbContext);
            var result = userPerms.ToList();
            foreach (Role role in roles)
            {
                result.AddRange(await GetRolePermissionsExceptAsync(role, result, dbContext));
            }
            return result.AsEnumerable();
        }

        public Task<IEnumerable<Permission>> GetRolePermissionsAsync(long roleId, BotDbContext dbContext)
        {
            var result = dbContext.Roles.Where(r => r.Id == roleId).SelectMany(r => r.RolePermissions).Select(p => p.Permission).AsEnumerable();
            return Task.FromResult(result);
        }

        public Task<IEnumerable<Permission>> GetRolePermissionsExceptAsync(Role role, IEnumerable<Permission> Except, BotDbContext dbContext)
        {
            var result = dbContext.Roles.Where(r => r == role).SelectMany(r => r.RolePermissions).Select(p => p.Permission).Except(Except).AsEnumerable();
            return Task.FromResult(result);
        }

        public Task<IEnumerable<Role>> GetRolesAsync(string discordUserId, BotDbContext dbContext)
        {
            var result = dbContext.Users.Where(u => u.DiscordUserId == discordUserId).SelectMany(u => u.Roles).Select(r => r.Role).AsEnumerable();
            return Task.FromResult(result);
        }

        public Task<IEnumerable<Permission>> GetUserPermissionsAsync(string discordUserId, BotDbContext dbContext)
        {
            var result = dbContext.Users.Where(u => u.DiscordUserId == discordUserId).SelectMany(u => u.UserPermissions).Select(p => p.Permission).AsEnumerable();
            return Task.FromResult(result);
        }

        public async Task<bool> IsAllowedAsync(PermissionType permissionType, SocketUser user)
        {
            if (await IsBotAdminAsync(user))
            {
                return true;
            }
            else
            {
                using (var dbContext = new BotDbContext())
                {
                    var perms = await GetAllPermissionsToAUserAsync(user.Id.ToString(), dbContext);
                    var result = perms.Select(p => p.Id).Contains((long)permissionType);
                    if (!result)
                    {
                        _logger.LogInformation($"Permission: '{permissionType.ToString()}' denied for user with id '{user.Id}'");
                    }
                    return result;
                }
                
            }
        }

        public Task<bool> IsBotAdminAsync(SocketUser user)
        {
            return Task.FromResult(_config.DiscordBotAdminUser == user.Id.ToString());
        }
    }
}
