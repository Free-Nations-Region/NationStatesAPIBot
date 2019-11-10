using CyborgianStates.Interfaces;
using CyborgianStates.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyborgianStates.Repositories
{
    public class UserRepository : IUserRepository
    {
        IMongoCollection<User> users;
        AppSettings _config;
        public UserRepository(IMongoDatabase database, IOptions<AppSettings> config)
        {
            if(database == null)
            {
                throw new ArgumentNullException(nameof(database));
            }
            if(config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            users = database.GetCollection<User>("user");
            _config = config.Value;
        }

        public async Task AddUserToDbAsync(ulong userId)
        {
            var user = new User() { DiscordUserId = userId };
            user.Permissions.Add(new Permission() { Name = "ExecuteCommands", CreatedAt = DateTime.UtcNow });
            await users.InsertOneAsync(user);
        }

        public async Task<bool> IsUserInDbAsync(ulong userId)
        {
            var res = await users.FindAsync(u => u.DiscordUserId == userId);
            var user = await res.FirstOrDefaultAsync();
            return user != null;
        }

        public async Task RemoveUserFromDbAsync(ulong userId)
        {
            await users.FindOneAndDeleteAsync(u => u.DiscordUserId == userId);
        }

        public async Task<bool> IsAllowedAsync(string permissionType, ulong userId)
        {
            var res = await users.FindAsync(u => u.DiscordUserId == userId);
            var user = await res.FirstOrDefaultAsync();
            if(user != null)
            {
                return user.Permissions.Any(p => p.Name == permissionType);
            }
            else
            {
                return false;
            }
        }

        public Task<bool> IsBotAdminAsync(ulong userId)
        {
            return Task.FromResult(_config.DiscordBotAdminUser == userId);
        }
    }
}
