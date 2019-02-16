using Discord;
using Discord.Commands;
using NationStatesAPIBot.Managers;
using NationStatesAPIBot.Types;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NationStatesAPIBot.Commands.Management
{
    public class RefreshPending : ModuleBase<SocketCommandContext>
    {
        static readonly string actionQueued = $"The action was queued successfully. Please be patient this may take a moment.";
        [Command("refresh"), Summary("Fetches new and rejected nations and adds them to pending")]
        public async Task DoRefreshPending([Remainder, Summary("The type to be fetched")] string type)
        {
            try
            {
                if (PermissionManager.IsAllowed(PermissionType.AccessPending, Context.User))
                {
                    
                    if (type == "all" || string.IsNullOrWhiteSpace(type))
                    {
                        await ReplyAsync(actionQueued);
                        var syncResult = await HandleRejected();
                        var result = await HandleNew();
                        await ReplyAsync($"<@{Context.User.Id}> You action just finished. Database was synced. {syncResult.Item1 + result} Nations added to database. {syncResult.Item2} removed from database."); //To-Do Send embeded
                    }
                    else if (type == "new")
                    {
                        await ReplyAsync(actionQueued);
                        var result = await HandleNew();
                        await ReplyAsync($"<@{Context.User.Id}> You action just finished. Database was synced. {result} Nations added to pending. {result} Nations added to database."); //To-Do: Send embeded
                    }
                    else if (type == "rejected")
                    {
                        await ReplyAsync(actionQueued);
                        var syncResult = await HandleRejected();
                        await ReplyAsync($"<@{Context.User.Id}> You action just finished. Database was synced. {syncResult.Item1} Nations added to pending. {syncResult.Item1} Nations added to database. {syncResult.Item2} removed from database."); //To-Do Send embeded
                    }
                    else
                    {
                        await ReplyAsync($"Don't know what you mean. '{type}' is unknown for this command.");
                    }
                }
                else
                {
                    await ReplyAsync(ActionManager.PERMISSION_DENIED_RESPONSE);
                }
            }
            catch (Exception ex)
            {
                await ActionManager.LoggerInstance.LogAsync(LogSeverity.Error, "RefreshPending", ex.ToString());
            }
        }

        public async Task<Tuple<int, int>> HandleRejected()
        {
            string regionName = "the rejected realms";
            var result = await ActionManager.NationStatesApiController.RequestNationsFromRegionAsync(regionName, false);
            var joined = ActionManager.NationStatesApiController.MatchNationsAgainstKnownNations(result, "member", regionName);
            var syncResult = await ActionManager.NationStatesApiController.SyncRegionMembersWithDatabase(result, regionName);
            using (var dbContext = new BotDbContext())
            {
                if (dbContext.Nations.Count() > 0)
                {
                    await ActionManager.NationStatesApiController.AddToPending(joined);
                }
            }
            return syncResult;
        }

        public async Task<int> HandleNew()
        {
            await ActionManager.LoggerInstance.LogAsync(LogSeverity.Debug, "RefreshPending - HandleNew", "Entered");
            var result = await ActionManager.NationStatesApiController.RequestNewNationsAsync(false);
            await ActionManager.LoggerInstance.LogAsync(LogSeverity.Debug, "RefreshPending - HandleNew", "Nations requested");
            var newnations = ActionManager.NationStatesApiController.MatchNationsAgainstKnownNations(result, "pending");
            await ActionManager.LoggerInstance.LogAsync(LogSeverity.Debug, "RefreshPending - HandleNew", "Nations matched");
            await ActionManager.NationStatesApiController.AddToPending(newnations);
            await ActionManager.LoggerInstance.LogAsync(LogSeverity.Debug, "RefreshPending - HandleNew", $"{newnations.Count} Added to Pending - done");
            return newnations.Count;
        }
    }
}
