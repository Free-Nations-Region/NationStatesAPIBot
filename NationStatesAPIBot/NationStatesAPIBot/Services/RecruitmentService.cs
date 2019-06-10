using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NationStatesAPIBot.Commands.Management;
using NationStatesAPIBot.Entities;
using NationStatesAPIBot.Manager;
using NationStatesAPIBot.Types;

namespace NationStatesAPIBot.Services
{
    public class RecruitmentService
    {
        private readonly ILogger<RecruitmentService> _logger;


        private RNStatus currentRNStatus;
        private EventId defaulEventId;
        private AppSettings _appSettings;
        private NationStatesApiService _apiService;
        public RecruitmentService(ILogger<RecruitmentService> logger, IOptions<AppSettings> appSettings)
        {
            _logger = logger;
            _appSettings = appSettings.Value;
            defaulEventId = LogEventIdProvider.GetEventIdByType(LoggingEvent.APIRecruitment);
        }

        public RecruitmentService(ILogger<RecruitmentService> logger, IOptions<AppSettings> appSettings, NationStatesApiService apiService)
        {
            _logger = logger;
            _appSettings = appSettings.Value;
            _apiService = apiService;
            defaulEventId = LogEventIdProvider.GetEventIdByType(LoggingEvent.APIRecruitment);
        }

        public bool IsReceivingRecruitableNation { get; internal set; }
        public bool IsRecruiting { get; private set; }

        public void StartRecruitment()
        {
            IsRecruiting = true;
            Task.Run(async () => await GetNewNationsAsync());
            Task.Run(async () => await RecruitAsync());
            _logger.LogInformation(defaulEventId, LogMessageBuilder.Build(defaulEventId, "Recruitment process started."));    
        }

        public void StopRecruitment()
        {
            IsRecruiting = false;
            _logger.LogInformation(defaulEventId, LogMessageBuilder.Build(defaulEventId, "Recruitment process stopped."));
        }

        public void StartReceiveRecruitableNations(RNStatus currentRN)
        {
            currentRNStatus = currentRN;
            IsReceivingRecruitableNation = true;
        }
        public void StopReceiveRecruitableNations()
        {
            currentRNStatus = null;
            IsReceivingRecruitableNation = false;
        }

        public async Task<List<Nation>> GetRecruitableNationsAsync(int number)
        {
            throw new NotImplementedException();
        }

        private async Task<bool> DoesNationFitCriteriaAsync(Nation nation)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> WouldReceiveTelegram(string nationName)
        {
            XmlDocument result = await _apiService.GetWouldReceiveTelegramAsync(nationName);
            if (result != null)
            {
                XmlNodeList canRecruitNodeList = result.GetElementsByTagName("TGCANRECRUIT");
                return canRecruitNodeList[0].InnerText == "1";
            }
            else
            {
                return false;
            }
        }

        private async Task GetNewNationsAsync()
        {
            throw new NotImplementedException();
        }

        private async Task RecruitAsync()
        {
            List<Nation> pendingNations = new List<Nation>();
            while (IsRecruiting)
            {
                try
                {
                    if (pendingNations.Count == 0)
                    {
                        pendingNations = NationManager.GetNationsByStatusName("reserved_api");
                        if (pendingNations.Count < 10)
                        {
                            pendingNations = await GetRecruitableNationsAsync(10 - pendingNations.Count);
                            foreach (var pendingNation in pendingNations)
                            {
                                await NationManager.SetNationStatusToAsync(pendingNation, "reserved_api");
                            }
                        }
                    }
                    var picked = pendingNations.Take(1);
                    var nation = picked.Count() > 0 ? picked.ToArray()[0] : null;
                    if (nation != null)
                    {
                        if (await _apiService.IsNationStatesApiActionReadyAsync(NationStatesApiRequestType.SendRecruitmentTelegram, true))
                        {
                            if (await WouldReceiveTelegram(nation.Name))
                            {
                                if (await _apiService.SendRecruitmentTelegramAsync(nation.Name))
                                {
                                    await NationManager.SetNationStatusToAsync(nation, "send");
                                    _logger.LogInformation(defaulEventId, LogMessageBuilder.Build(defaulEventId, $"Telegram to {nation.Name} queued successfully."));
                                }
                                else
                                {
                                    await NationManager.SetNationStatusToAsync(nation, "failed");
                                }
                                pendingNations.Remove(nation);
                            }
                            else
                            {
                                await NationManager.SetNationStatusToAsync(nation, "skipped");
                                pendingNations.Remove(nation);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(defaulEventId, LogMessageBuilder.Build(defaulEventId, "An error occured."), ex);
                }
                await Task.Delay(10000);
            }
        }

        internal string GetRNStatus()
        {
            if (IsReceivingRecruitableNation)
            {
                return currentRNStatus.ToString();
            }
            else
            {
                return "No /rn command currently running.";
            }
        }
    }
}
