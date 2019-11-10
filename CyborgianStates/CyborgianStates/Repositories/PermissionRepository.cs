using CyborgianStates.Interfaces;
using CyborgianStates.Models;
using CyborgianStates.Types;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CyborgianStates.Repositories
{
    public class PermissionRepository : IPermissionRepository
    {


        public Task AddPermissionAsync(string discordUserId, Role role)
        {
            throw new NotImplementedException();
        }

        public Task AddPermissionToRoleAsync(Role role, Permission permission)
        {
            throw new NotImplementedException();
        }

        public Task AddRoleAsync(string roleDescription)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Permission>> GetAllPermissionsToAUserAsync(string discordUserId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Permission>> GetRolePermissionsAsync(long roleId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Role>> GetRolesAsync(string discordUserId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Permission>> GetUserPermissionsAsync(string discordUserId)
        {
            throw new NotImplementedException();
        }

        

        public Task RemoveRoleAsync(Role role)
        {
            throw new NotImplementedException();
        }

        public Task RevokePermissionAsync(string discordUserId, Permission permission)
        {
            throw new NotImplementedException();
        }

        public Task RevokePermissionFromRoleAsync(Role role, Permission permission)
        {
            throw new NotImplementedException();
        }
    }
}
