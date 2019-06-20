using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NationStatesAPIBot.Entities;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NationStatesAPIBot.Types;

namespace NationStatesAPIBot.Manager
{
    //This class isn't static because of the requirement of using it as type for receiving logger
    public class UserManager 
    {
        private static AppSettings _config;
        private static ILogger<UserManager> logger;
        public static void Initialize(AppSettings config)
        {
            _config = config;
            logger = Program.ServiceProvider.GetService<ILogger<UserManager>>();
        }
        public static async Task<bool> IsUserInDb(string userId)
        {
            using (var context = new BotDbContext(_config))
            {
                return await context.Users.FirstOrDefaultAsync(u => u.DiscordUserId == userId) != null;
            }
        }
        public static async Task AddUserToDbAsync(string userId)
        {
            if (!await IsUserInDb(userId)) //check
            {
                var id = LogEventIdProvider.GetEventIdByType(LoggingEvent.UserDbAction);
                using (var dbContext = new BotDbContext(_config))
                {
                    var user = new User() { DiscordUserId = userId };
                    await dbContext.Users.AddAsync(user);
                    await dbContext.SaveChangesAsync();

                    var defaultUserRole = await dbContext.Roles.FirstOrDefaultAsync(r => r.Description == "Default-User");
                    if (defaultUserRole == null)
                    {
                        defaultUserRole = new Role() { Description = "Default-User" };
                        await dbContext.Roles.AddAsync(defaultUserRole);
                    }
                    var userRole = new UserRoles() { User = user, Role = defaultUserRole, RoleId = defaultUserRole.Id };
                    user.Roles.Add(userRole);
                    dbContext.Users.Update(user);
                    await dbContext.SaveChangesAsync();
                    logger.LogInformation(id, LogMessageBuilder.Build(id, $"Added User {userId} to database"));
                }
            }
        }
        public static async Task RemoveUserFromDbAsync(string userId)
        {
            using (var dbContext = new BotDbContext(_config))
            {
                var user = await dbContext.Users.FirstOrDefaultAsync(u => u.DiscordUserId == userId);
                if (user != null)
                {
                    var id = LogEventIdProvider.GetEventIdByType(LoggingEvent.UserDbAction);
                    dbContext.Remove(user);
                    await dbContext.SaveChangesAsync();
                    logger.LogInformation(id, LogMessageBuilder.Build(id, $"Removed User {userId} from database"));
                }
            }
        }


    }
}
