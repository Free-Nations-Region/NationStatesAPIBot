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

        public async Task<List<Nation>> GetRecruitableNations(int number)
        {
            throw new NotImplementedException();
        }

        public async Task SetNationStatusToAsync(Nation nation, string statusName)
        {
            throw new NotImplementedException();
        }


        private async Task<bool> WouldReceiveTelegram(string nationName)
        {
            XmlDocument result = await _apiService.WouldReceiveTelegramAsync(nationName);
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

        private async void GetNewNationsAsync()
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
                        pendingNations = GetNationsByStatusName("reserved_api");
                        if (pendingNations.Count < 10)
                        {
                            pendingNations = await GetRecruitableNations(10 - pendingNations.Count);
                            foreach (var pendingNation in pendingNations)
                            {
                                await SetNationStatusToAsync(pendingNation, "reserved_api");
                            }
                        }
                    }
                    var picked = pendingNations.Take(1);
                    var nation = picked.Count() > 0 ? picked.ToArray()[0] : null;
                    if (nation != null)
                    {
                        if (await WouldReceiveTelegram(nation.Name))
                        {
                            if (await _apiService.SendRecruitmentTelegramAsync(nation.Name))
                            {
                                await SetNationStatusToAsync(nation, "send");
                            }
                            else
                            {
                                await SetNationStatusToAsync(nation, "failed");
                                _logger.LogWarning(defaulEventId, LogMessageBuilder.Build(defaulEventId, $"Telegram to {nation.Name} could not be send."));
                            }
                            pendingNations.Remove(nation);

                        }
                        else
                        {
                            await SetNationStatusToAsync(nation, "skipped");
                            pendingNations.Remove(nation);
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

        private async Task SendTelegramAsync()
        {
            throw new NotImplementedException();
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

        private List<Nation> GetNationsByStatusName(string name)
        {
            using (var dbContext = new BotDbContext())
            {
                return dbContext.Nations.Where(n => n.Status.Name == name).OrderByDescending(n => n.StatusTime).ToList();
            }
        }
    }
}
