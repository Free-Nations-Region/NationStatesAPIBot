using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
        private readonly EventId defaulEventId;
        private readonly AppSettings _config;
        private readonly NationStatesApiService _apiService;

        public RecruitmentService(ILogger<RecruitmentService> logger, IOptions<AppSettings> appSettings, NationStatesApiService apiService)
        {
            _logger = logger;
            _config = appSettings.Value;
            _apiService = apiService;
            defaulEventId = LogEventIdProvider.GetEventIdByType(LoggingEvent.APIRecruitment);
        }

        public bool IsReceivingRecruitableNations { get; internal set; }
        public static bool IsRecruiting { get; private set; }

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
            IsReceivingRecruitableNations = true;
        }
        public void StopReceiveRecruitableNations()
        {
            currentRNStatus = null;
            IsReceivingRecruitableNations = false;
        }

        public async Task<List<Nation>> GetRecruitableNationsAsync(int number)
        {
            List<Nation> returnNations = new List<Nation>();
            var id = LogEventIdProvider.GetEventIdByType(LoggingEvent.GetRecruitableNations);
            try
            {
                _logger.LogDebug(id, LogMessageBuilder.Build(id, $"{number} recruitable nations requested"));
                List<Nation> pendingNations = new List<Nation>();
                if (pendingNations.Count == 0)
                {
                    pendingNations = NationManager.GetNationsByStatusName("pending");
                }
                while (returnNations.Count < number)
                {
                    var picked = pendingNations.Take(1);
                    var nation = picked.Count() > 0 ? picked.ToArray()[0] : null;
                    if (nation != null)
                    {
                        while (!await DoesNationFitCriteriaAsync(nation))
                        {
                            _logger.LogDebug(id, LogMessageBuilder.Build(id, $"{nation.Name} does not fit criteria and is therefore skipped"));
                            pendingNations.Remove(nation);
                            await NationManager.SetNationStatusToAsync(nation, "skipped");
                            picked = pendingNations.Take(1);
                            nation = picked.Count() > 0 ? picked.ToArray()[0] : null;
                        }
                        while (!await WouldReceiveTelegram(nation.Name))
                        {
                            pendingNations.Remove(nation);
                            await NationManager.SetNationStatusToAsync(nation, "skipped");
                            picked = pendingNations.Take(1);
                            nation = picked.Count() > 0 ? picked.ToArray()[0] : null;
                            _logger.LogDebug(id, LogMessageBuilder.Build(id, $"Nation: {nation.Name} would not receive this recruitment telegram and is therefore skipped."));
                        }
                        pendingNations.Remove(nation);
                        returnNations.Add(nation);
                        if (IsReceivingRecruitableNations)
                        {
                            currentRNStatus.CurrentCount++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(id, ex, LogMessageBuilder.Build(id, "An error occured."));
            }
            return returnNations;

        }

        private async Task<bool> DoesNationFitCriteriaAsync(Nation nation)
        {
            if (_config.CriteriaCheckOnNations)
            {
                string pattern = @"(^[0-9]+?|[0-9]+?$)";
                return await Task.FromResult(!Regex.IsMatch(nation.Name, pattern));
            }
            else
            {
                return await Task.FromResult(true);
            }
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
                var id = LogEventIdProvider.GetEventIdByType(LoggingEvent.WouldReceiveTelegram);
                _logger.LogWarning(id, LogMessageBuilder.Build(id, "Result of GetWouldReceiveTelegramAsync were null"));
                return false;
            }
        }

        private async Task GetNewNationsAsync()
        {
            var id = LogEventIdProvider.GetEventIdByType(LoggingEvent.GetNewNations);
            while (IsRecruiting)
            {
                try
                {
                    await _apiService.WaitForAction(NationStatesApiRequestType.GetNewNations);
                    var result = await _apiService.GetNewNationsAsync(id);
                    var counter = await NationManager.AddUnknownNationsAsPendingAsync(result);
                    _logger.LogInformation(id, LogMessageBuilder.Build(id, $"{counter} nations added to pending"));
                }
                catch (Exception ex)
                {
                    _logger.LogError(id, ex, LogMessageBuilder.Build(id, "An error occured."));
                }
                await Task.Delay(300000);
            }
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
                                    _logger.LogWarning(defaulEventId, LogMessageBuilder.Build(defaulEventId, $"Sending of a Telegram to {nation.Name} failed."));
                                    await NationManager.SetNationStatusToAsync(nation, "failed");
                                }
                                pendingNations.Remove(nation);
                            }
                            else
                            {
                                _logger.LogDebug(defaulEventId, LogMessageBuilder.Build(defaulEventId, $"Nation: {nation.Name} wouldn't receive an recruitment telegram and is therefore skipped."));
                                await NationManager.SetNationStatusToAsync(nation, "skipped");
                                pendingNations.Remove(nation);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(defaulEventId, ex, LogMessageBuilder.Build(defaulEventId, "An error occured."));
                }
                await Task.Delay(10000);
            }
        }

        internal string GetRNStatus()
        {
            if (IsReceivingRecruitableNations)
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
