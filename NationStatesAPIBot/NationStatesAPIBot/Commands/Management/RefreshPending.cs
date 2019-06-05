using Discord;
using Discord.Commands;
using NationStatesAPIBot.Managers;
using NationStatesAPIBot.Types;
using System;
using System.Threading.Tasks;

namespace NationStatesAPIBot.Commands.Management
{
    public class RefreshPending : ModuleBase<SocketCommandContext>
    {
        static readonly string actionQueued = $"The action was queued successfully. Please be patient this may take a moment.";
        //[Command("refresh"), Summary("Fetches new and rejected nations and adds them to pending")]
        public async Task DoRefreshPending()
        {
            //try
            //{
            //    if (PermissionManager.IsAllowed(PermissionType.AccessPending, Context.User))
            //    {
            //        await ReplyAsync(actionQueued);
            //        var result = await HandleNew();
            //        await ReplyAsync($"<@{Context.User.Id}> You action just finished. Database was synced. {result} Nations added to database. {result} Nations added to pending."); //To-Do: Send embeded
            //    }
            //    else
            //    {
            //        await ReplyAsync(ActionManager.PERMISSION_DENIED_RESPONSE);
            //    }
            //}
            //catch (Exception ex)
            //{
            //    await ActionManager.LoggerInstance.LogAsync(LogSeverity.Error, "RefreshPending", ex.ToString());
            //}
        }

        private async Task<int> HandleNew()
        {
            var result = await ActionManager.NationStatesApiController.RequestNewNationsAsync(false);
            var newnations = ActionManager.NationStatesApiController.MatchNationsAgainstKnownNations(result, "pending");
            await ActionManager.NationStatesApiController.AddToPending(newnations);
            return newnations.Count;
        }
    }
}
