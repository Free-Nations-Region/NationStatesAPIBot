using CyborgianStates.DumpData;
using CyborgianStates.Interfaces;
using CyborgianStates.Models;
using CyborgianStates.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace CyborgianStates.Services
{
    public class RecruitmentService
    {
        private readonly EventId defaulEventId;
        private readonly ILogger<RecruitmentService> _logger;
        private readonly NationStatesApiService _apiService;
        private readonly DumpDataService _dumpDataService;
        private readonly AppSettings _config;
        private readonly Random _rnd;
        private readonly INationRepository _nationRepository;
        public RecruitmentService(ILogger<RecruitmentService> logger, IOptions<AppSettings> appSettings, NationStatesApiService apiService, DumpDataService dumpDataService, INationRepository nationRepository)
        {
            _logger = logger;
            if (appSettings == null)
            {
                throw new ArgumentNullException(nameof(appSettings));
            }
            _config = appSettings.Value;
            _apiService = apiService;
            _dumpDataService = dumpDataService;
            defaulEventId = LogEventIdProvider.GetEventIdByType(LoggingEvent.APIRecruitment);
            _nationRepository = nationRepository;
            _rnd = new Random();
            if (!_config.EnableRecruitment)
            {
                RecruitmentStatus = "Disabled";
            }
        }
        public bool IsReceivingRecruitableNations { get; internal set; }
        public static bool IsRecruiting { get; private set; }
        public static string RecruitmentStatus { get; private set; } = "Not Running";
        public static string PoolStatus { get; private set; } = "Waiting for new nations";

        public void StartRecruitment()
        {
            if (!IsRecruiting)
            {
                IsRecruiting = true;
                Task.Run(async () => await GetNewNationsAsync());
                Task.Run(async () => await EnsurePoolFilled());
                Task.Run(async () => await RecruitAsync());
                RecruitmentStatus = "Started";
                //UpdateRecruitmentStatsAsync();
                _logger.LogInformation(defaulEventId, LogMessageBuilder.Build(defaulEventId, "Recruitment process started."));
            }
        }

        public void StopRecruitment()
        {
            IsRecruiting = false;
            RecruitmentStatus = "Stopped";
            _logger.LogInformation(defaulEventId, LogMessageBuilder.Build(defaulEventId, "Recruitment process stopped."));
        }

        private async Task GetNewNationsAsync()
        {
            var id = LogEventIdProvider.GetEventIdByType(LoggingEvent.GetNewNations);
            while (IsRecruiting)
            {
                try
                {
                    await _apiService.WaitForAction(NationStatesApiRequestType.GetNewNations);
                    PoolStatus = "Filling up with new nations";
                    var result = await _apiService.GetNewNationsAsync(id);
                    await AddNationToPendingAsync(id, result);
                    PoolStatus = "Waiting for new nations";
                }
                catch (Exception ex)
                {
                    _logger.LogError(id, ex, LogMessageBuilder.Build(id, "An error occured."));
                }
                await Task.Delay(300000);
            }
        }

        private async Task AddNationToPendingAsync(EventId id, List<string> nationNames)
        {
            List<string> nationsToAdd = new List<string>();
            foreach (var res in nationNames)
            {
                if (await DoesNationNameFitCriteriaAsync(res))
                {
                    nationsToAdd.Add(res);
                }
            }
            var counter = await _nationRepository.BulkAddNationsToPendingAsync(nationsToAdd);
            if (counter > 0)
            {
                _logger.LogInformation(id, LogMessageBuilder.Build(id, $"{counter} nations added to pending"));
            }
        }

        private async Task<bool> IsNationRecruitableAsync(string nationName, EventId id)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(nationName))
                {
                    var nation = await _nationRepository.GetNationAsync(nationName);
                    if (!await DoesNationNameFitCriteriaAsync(nationName) || !await WouldReceiveTelegram(nationName))
                    {
                        if (nation != null)
                        {
                            if (nation.Status.Any(s => s.Active && s.Name == "pending"))
                            {
                                await _nationRepository.SwitchNationStatusAsync(nation, "pending", "skipped", id);
                            }
                            else
                            {
                                await _nationRepository.SetNationStatusAsync(nation, "skipped", true);
                            }
                            _logger.LogDebug(id, LogMessageBuilder.Build(id, $"{nationName} does not fit criteria and is therefore skipped"));
                        }
                        else
                        {
                            _logger.LogWarning(id, LogMessageBuilder.Build(id, $"Nation '{nationName}' does not fit criteria and wasn't in the database therefore status couldn't be set to skipped."));
                        }
                        return false;
                    }
                    return true;
                }
                return false;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogCritical(id, ex, LogMessageBuilder.Build(id, "A critical error occured."));
                return false;
            }
        }

        private async Task<bool> DoesNationNameFitCriteriaAsync(string nationName)
        {
            if (_config.CriteriaCheckOnNations)
            {
                var res = !nationName.Any(c => char.IsDigit(c)) && nationName.Count(c => c == nationName[0]) != nationName.Length;
                _logger.LogDebug($"{nationName} criteria fit: {res}");
                return await Task.FromResult(res);
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
                _logger.LogWarning(id, LogMessageBuilder.Build(id, $"Result of GetWouldReceiveTelegramAsync '{nationName}' were null"));
                return false;
            }
        }

        private async Task EnsurePoolFilled()
        {
            var id = LogEventIdProvider.GetEventIdByType(LoggingEvent.EnsurePoolFilled);
            List<REGION> regionsToRecruitFrom = await GetRegionToRecruitFrom(id);
            while (IsRecruiting)
            {
                bool fillingUp = false;
                int counter = 0;
                int pendingCount = await _nationRepository.GetNationCountByStatusNameAsync("pending");
                while (pendingCount < _config.MinimumRecruitmentPoolSize)
                {
                    if (!fillingUp)
                    {
                        fillingUp = true;
                        _logger.LogInformation(id, LogMessageBuilder.Build(id, $"Filling up pending pool now from {pendingCount} to {_config.MinimumRecruitmentPoolSize}"));
                    }
                    PoolStatus = "Filling up with random nations";
                    var regionId = _rnd.Next(regionsToRecruitFrom.Count);
                    var region = regionsToRecruitFrom.ElementAt(regionId);
                    string nationName;
                    do
                    {
                        var nationId = _rnd.Next(region.NATIONNAMES.Count);
                        nationName = region.NATIONNAMES.ElementAt(nationId);
                    }
                    while (await IsOrWasNationPending(nationName) || !await IsNationRecruitableAsync(nationName, id));
                    var nation = await _nationRepository.GetNationAsync(nationName);
                    if (nation != null)
                    {
                        await _nationRepository.SetNationStatusAsync(nation, "pending", true);
                    }
                    else
                    {
                        await _nationRepository.BulkAddNationsToPendingAsync(new List<string>() { nationName });
                    }
                    counter++;
                    pendingCount = await _nationRepository.GetNationCountByStatusNameAsync("pending");
                    _logger.LogDebug(id, LogMessageBuilder.Build(id, $"Added nation '{nationName}' to pending. Now at {pendingCount} from minimum {_config.MinimumRecruitmentPoolSize}."));
                }
                if (fillingUp)
                {
                    _logger.LogInformation(id, LogMessageBuilder.Build(id, $"Filled up pending pool to minimum. (Added {counter} nations to pending.)"));
                    PoolStatus = "Waiting for new nations";
                }

                await Task.Delay(1800000); //30 min
            }
        }

        private async Task<bool> IsOrWasNationPending(string nationName)
        {
            var nation = await _nationRepository.GetNationAsync(nationName);
            /* 
             * Check for active and inactive status to assure 
             * nations are only handled once for recruitment
             */
            return nation.Status.Any(s => s.Name == "pending");
        }

        private async Task<List<REGION>> GetRegionToRecruitFrom(EventId id)
        {
            List<REGION> regionsToRecruitFrom = new List<REGION>();
            var regionNames = _config.RegionsToRecruitFrom.Split(";");
            foreach (var regionName in regionNames)
            {
                if (!string.IsNullOrWhiteSpace(regionName))
                {
                    var region = await _dumpDataService.GetRegionAsync(BaseApiService.ToID(regionName));
                    if (region != null)
                    {
                        regionsToRecruitFrom.Add(region);
                        _logger.LogInformation(id, LogMessageBuilder.Build(id, $"Region '{regionName}' added to regionsToRecruitFrom."));
                    }
                    else
                    {
                        _logger.LogWarning(id, LogMessageBuilder.Build(id, $"Region for name '{regionName}' couldn't be found in dumps."));
                    }
                }
            }

            return regionsToRecruitFrom;
        }

        private async Task RecruitAsync()
        {
            List<Nation> pendingNations = new List<Nation>();
            while (IsRecruiting && _config.EnableRecruitment)
            {
                try
                {
                    if (pendingNations.Count == 0)
                    {
                        if (await _nationRepository.GetNationCountByStatusNameAsync("pending") == 0)
                        {
                            _logger.LogWarning("Delaying API recruitment for 15 minutes due to lack of recruitable nations");
                            RecruitmentStatus = "Throttled: lack of nations";
                            await Task.Delay(900000);
                        }
                        pendingNations = await _nationRepository.GetNationsByStatusNameAsync("reserved_api");
                        if (pendingNations.Count < 10)
                        {
                            var numberToRequest = 10 - pendingNations.Count;
                            pendingNations = await GetRecruitableNationsAsync(numberToRequest, true);
                            if (pendingNations.Count < numberToRequest)
                            {
                                RecruitmentStatus = "Throttled: lack of of nations";
                                _logger.LogWarning("Didn't received enough recruitable nations");
                            }
                            else
                            {
                                RecruitmentStatus = "Fully operational";
                            }
                            foreach (var pendingNation in pendingNations)
                            {
                                await _nationRepository.SwitchNationStatusAsync(pendingNation, "pending", "reserved_api", LogEventIdProvider.GetRandomLogEventId());
                            }
                        }
                    }
                    var picked = pendingNations.Take(1);
                    var nation = picked.Any() ? picked.ToArray()[0] : null;
                    if (nation != null)
                    {
                        if (await _apiService.IsNationStatesApiActionReadyAsync(NationStatesApiRequestType.SendRecruitmentTelegram, true))
                        {
                            if (await IsNationRecruitableAsync(nation.Name, defaulEventId))
                            {
                                if (await _apiService.SendRecruitmentTelegramAsync(nation.Name))
                                {
                                    await _nationRepository.SwitchNationStatusAsync(nation, "reserved_api", "send", LogEventIdProvider.GetRandomLogEventId());
                                    _logger.LogInformation(defaulEventId, LogMessageBuilder.Build(defaulEventId, $"Telegram to {nation.Name} queued successfully."));
                                }
                                else
                                {
                                    await _nationRepository.SwitchNationStatusAsync(nation, "reserved_api", "failed", LogEventIdProvider.GetRandomLogEventId());
                                    _logger.LogWarning(defaulEventId, LogMessageBuilder.Build(defaulEventId, $"Sending of a Telegram to {nation.Name} failed."));
                                }
                            }
                            pendingNations.Remove(nation);
                        }
                    }
                    else
                    {
                        _logger.LogCritical(defaulEventId, LogMessageBuilder.Build(defaulEventId, "No nation to recruit found."));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(defaulEventId, ex, LogMessageBuilder.Build(defaulEventId, "An error occured."));
                }
                await Task.Delay(60000);
            }
            if (!_config.EnableRecruitment)
            {
                _logger.LogWarning(defaulEventId, "Recruitment disabled.");
            }
        }

        public async Task<List<Nation>> GetRecruitableNationsAsync(int number, bool isAPI)
        {
            List<Nation> returnNations = new List<Nation>();
            var id = LogEventIdProvider.GetEventIdByType(LoggingEvent.GetRecruitableNations);
            try
            {
                _logger.LogDebug(id, LogMessageBuilder.Build(id, $"{number} recruitable nations requested"));
                List<Nation> pendingNations = new List<Nation>();
                if (pendingNations.Count == 0)
                {
                    pendingNations = await _nationRepository.GetNationsByStatusNameAsync("pending");
                }
                while (returnNations.Count < number)
                {
                    var picked = pendingNations.Take(1);
                    var nation = picked.Any() ? picked.ToArray()[0] : null;
                    if (nation != null)
                    {
                        if (await IsNationRecruitableAsync(nation.Name, id))
                        {
                            returnNations.Add(nation);
                            if (IsReceivingRecruitableNations && !isAPI)
                            {
                                // TODO: Find a better way for RNStatus
                               currentRNStatus.CurrentCount++;
                            }
                        }
                        pendingNations.Remove(nation);
                        returnNations = returnNations.Distinct().ToList();
                    }
                    else
                    {
                        if (pendingNations.Count == 0)
                        {
                            _logger.LogCritical(id, LogMessageBuilder.Build(id, "No more nations in pending pool !"));
                            return returnNations;
                        }
                        else
                        {
                            _logger.LogCritical(id, "Picked nation was null !");
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
    }
}
