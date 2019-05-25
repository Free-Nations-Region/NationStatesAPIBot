using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NationStatesAPIBot.Interfaces
{
    public interface IBotService
    {
        Task RunAsync();
        Task IsRelevantAsync(object message, object user);
        Task ProcessMessageAsync(object message);
        bool IsRunning { get; }
    }
}
