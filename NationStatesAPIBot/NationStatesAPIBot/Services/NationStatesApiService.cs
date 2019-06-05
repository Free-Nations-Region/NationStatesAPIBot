using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NationStatesAPIBot.Interfaces;
using NationStatesAPIBot.Types;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NationStatesAPIBot.Services
{
    public class NationStatesApiService : BaseApiService
    {
        public const int API_VERSION = 9;
        public const long API_REQUEST_INTERVAL = 6000000; //0,6 s
        public const long SEND_NON_RECRUITMENTTELEGRAM_INTERVAL = 300000000; //30 s
        public const long SEND_RECRUITMENTTELEGRAM_INTERVAL = 1800000000; //3 m 1800000000
        public const long REQUEST_NEW_NATIONS_INTERVAL = 18000000000; //30 m 36000000000
        public const long REQUEST_REGION_NATIONS_INTERVAL = 432000000000; //12 h 432000000000

        public NationStatesApiService(IOptions<AppSettings> config, ILogger<NationStatesApiService> logger) : base(config, logger) { }

        public DateTime LastAPIRequest { get => lastAPIRequest; private set => lastAPIRequest = value; }
        public DateTime LastTelegramSending { get => lastTelegramSending; set => lastTelegramSending = value; }
        public DateTime LastAutomaticNewNationsRequest { get => lastAutomaticNewNationsRequest; set => lastAutomaticNewNationsRequest = value; }
        public DateTime LastAutomaticRegionNationsRequest { get => lastAutomaticRegionNationsRequest; set => lastAutomaticRegionNationsRequest = value; }

        public Task<bool> IsNationStatesApiActionReadyAsync(NationStatesApiRequestType type, bool isScheduledAction)
        {
            if (type == NationStatesApiRequestType.GetNationsFromRegion)
            {
                return Task.FromResult(DateTime.UtcNow.Ticks - LastAutomaticRegionNationsRequest.Ticks > (isScheduledAction ? REQUEST_REGION_NATIONS_INTERVAL : API_REQUEST_INTERVAL));
            }
            else if (type == NationStatesApiRequestType.GetNewNations)
            {
                return Task.FromResult(DateTime.UtcNow.Ticks - LastAutomaticNewNationsRequest.Ticks > (isScheduledAction ? REQUEST_NEW_NATIONS_INTERVAL : API_REQUEST_INTERVAL));
            }
            else if (type == NationStatesApiRequestType.SendRecruitmentTelegram)
            {
                return Task.FromResult(DateTime.UtcNow.Ticks - LastTelegramSending.Ticks > SEND_RECRUITMENTTELEGRAM_INTERVAL);
            }
            else if (type == NationStatesApiRequestType.GetNationStats || type == NationStatesApiRequestType.GetRegionStats || type == NationStatesApiRequestType.WouldReceiveRecruitmentTelegram)
            {
                return Task.FromResult(DateTime.UtcNow.Ticks - LastAPIRequest.Ticks > API_REQUEST_INTERVAL);
            }
            else
            {
                return Task.FromResult(false);
            }
        }
    }
}
