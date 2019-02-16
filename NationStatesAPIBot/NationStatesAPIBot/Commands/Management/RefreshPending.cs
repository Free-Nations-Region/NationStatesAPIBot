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
                        await ReplyAsync($"<@{Context.User.Id}> You action just finished. Database was synced. {syncResult.Item1 + result} Nations added to database. {syncResult.Item3 + result} Nations added to pending. {syncResult.Item2} removed from database."); //To-Do Send embeded
                    }
                    else if (type == "new")
                    {
                        await ReplyAsync(actionQueued);
                        var result = await HandleNew();
                        await ReplyAsync($"<@{Context.User.Id}> You action just finished. Database was synced. {result} Nations added to database. {result} Nations added to pending."); //To-Do: Send embeded
                    }
                    else if (type == "rejected")
                    {
                        await ReplyAsync(actionQueued);
                        var syncResult = await HandleRejected();
                        await ReplyAsync($"<@{Context.User.Id}> You action just finished. Database was synced. {syncResult.Item1} Nations added to database. {syncResult.Item3} Nations added to pending. {syncResult.Item2} removed from database."); //To-Do Send embeded
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

        /// <summary>
        /// Request nations of the region "the rejected realms" syncs them to the database and adds new ones to pending
        /// </summary>
        /// <returns>A Tuple of 1: Added 2: Removed from Database 3: Added to pending</returns>
        private async Task<Tuple<int, int, int>> HandleRejected()
        {
            using (var dbContext = new BotDbContext())
            {
                string regionName = "the rejected realms";
                int initialMemberCount = dbContext.Nations.Where(n => n.Status.Name == "member").Count();
                var result = await ActionManager.NationStatesApiController.RequestNationsFromRegionAsync(regionName, false);
                var joined = ActionManager.NationStatesApiController.MatchNationsAgainstKnownNations(result, "member", regionName);
                var syncResult = await ActionManager.NationStatesApiController.SyncRegionMembersWithDatabase(result, regionName);
                int addedToPending = 0;
                if (initialMemberCount > 0)
                {
                    await ActionManager.NationStatesApiController.AddToPending(joined);
                    addedToPending = joined.Count;
                }
                return new Tuple<int, int, int>(syncResult.Item1, syncResult.Item2, addedToPending);
            }
        }

        private async Task<int> HandleNew()
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
