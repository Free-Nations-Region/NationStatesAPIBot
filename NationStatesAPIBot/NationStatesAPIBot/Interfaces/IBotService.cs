using System.Threading.Tasks;

namespace NationStatesAPIBot.Interfaces
{
    public interface IBotService
    {
        Task RunAsync();
        Task<bool> IsRelevantAsync(object message, object user);
        Task ProcessMessageAsync(object message);
        bool IsRunning { get; }
        Task ShutdownAsync();
    }
}
