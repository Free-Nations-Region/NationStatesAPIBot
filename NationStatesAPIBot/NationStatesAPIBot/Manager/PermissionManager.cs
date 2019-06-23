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
using Microsoft.EntityFrameworkCore;

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

        public async Task RevokePermissionAsync(string discordUserId, Permission permission, BotDbContext dbContext)
        {
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.DiscordUserId == discordUserId);
            if (user == null)
            {
                var id = LogEventIdProvider.GetEventIdByType(LoggingEvent.UserDbAction);
                _logger.LogWarning(id, LogMessageBuilder.Build(id, $"Revoke Permission: DiscordUserId '{discordUserId}' not found in DB"));
                return;
            }
            var perm = user.UserPermissions.FirstOrDefault(p => p.Permission.Id == permission.Id);
            if (perm == null)
            {
                var id = LogEventIdProvider.GetEventIdByType(LoggingEvent.UserDbAction);
                _logger.LogWarning(id, LogMessageBuilder.Build(id, $"Revoke Permission: Permission.Id '{permission.Id}' not found in user.UserPermissions"));
                return;
            }
            var update = dbContext.Users.Update(user);
            update.Entity.UserPermissions.Remove(perm);
            await dbContext.SaveChangesAsync();
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
                using (var dbContext = new BotDbContext(_config))
                {
                    var perms = await GetAllPermissionsToAUserAsync(user.Id.ToString(), dbContext);
                    var result = perms.Select(p => p.Id).Contains((long)permissionType);
                    if (!result)
                    {
                        var id = LogEventIdProvider.GetEventIdByType(LoggingEvent.PermissionDenied);
                        _logger.LogInformation(id, LogMessageBuilder.Build(id, $"Permission: '{permissionType.ToString()}' denied for user with id '{user.Id}'"));
                    }
                    return result;
                }

            }
        }

        public Task<bool> IsBotAdminAsync(SocketUser user)
        {
            return Task.FromResult(_config.DiscordBotAdminUser == user.Id);
        }
    }
}
