using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NationStatesAPIBot.Commands.Management;
using NationStatesAPIBot.DumpData;
using NationStatesAPIBot.Entities;
using NationStatesAPIBot.Manager;
using NationStatesAPIBot.Types;
using Newtonsoft.Json.Linq;

namespace NationStatesAPIBot.Services
{
    public class RecruitmentService
    {
        private readonly ILogger<RecruitmentService> _logger;

        private RNStatus currentRNStatus;
        private readonly EventId _defaulEventId;
        private readonly AppSettings _config;
        private readonly NationStatesApiService _apiService;
        private readonly DumpDataService _dumpDataService;
        private readonly Random _rnd;
        public int ApiSent { get; private set; }
        public int ApiPending { get; private set; }
        public int ApiSkipped { get; private set; }
        public int ApiFailed { get; private set; }
        public int ApiRecruited { get; private set; }
        public double ApiRatio { get; private set; }
        public int ManualReserved { get; private set; }
        public int ManualRecruited { get; private set; }
        public double ManualRatio { get; private set; }
        public int RecruitedTodayA { get; private set; }
        public int RecruitedTodayM { get; private set; }
        public int RecruitedYesterdayA { get; private set; }
        public int RecruitedYesterdayM { get; private set; }
        public int RecruitedLastWeekA { get; private set; }
        public int RecruitedLastWeekM { get; private set; }
        public double RecruitedLastWeekAvgDA { get; private set; }
        public double RecruitedLastWeekAvgDM { get; private set; }
        public int RecruitedLastMonthA { get; private set; }
        public int RecruitedLastMonthM { get; private set; }
        public double RecruitedLastMonthAvgDA { get; private set; }
        public double RecruitedLastMonthAvgDM { get; private set; }

        public RecruitmentService(ILogger<RecruitmentService> logger, IOptions<AppSettings> appSettings, NationStatesApiService apiService, DumpDataService dumpDataService)
        {
            _logger = logger;
            _config = appSettings.Value;
            _apiService = apiService;
            _dumpDataService = dumpDataService;
            _defaulEventId = LogEventIdProvider.GetEventIdByType(LoggingEvent.APIRecruitment);
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
                Task.Run(async () => await EnsurePoolFilledAsync());
                Task.Run(async () => await RecruitAsync());
                RecruitmentStatus = "Started";
                Task.Run(async () => await UpdateRecruitmentStatsAsync());
                _logger.LogInformation(_defaulEventId, LogMessageBuilder.Build(_defaulEventId, "Recruitment process started."));
            }
        }

        public void StopRecruitment()
        {
            IsRecruiting = false;
            RecruitmentStatus = "Stopped";
            _logger.LogInformation(_defaulEventId, LogMessageBuilder.Build(_defaulEventId, "Recruitment process stopped."));
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
                    pendingNations = NationManager.GetNationsByStatusName("pending");
                }
                while (returnNations.Count < number)
                {
                    var picked = pendingNations.Take(1);
                    var nation = picked.Count() > 0 ? picked.ToArray()[0] : null;
                    if (nation != null)
                    {
                        if (await IsNationRecruitableAsync(nation, id))
                        {
                            returnNations.Add(nation);
                            if (IsReceivingRecruitableNations && !isAPI)
                            {
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
                            _logger.LogCritical(id, "No more nations in pending pool !");
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

        private async Task<bool> IsNationRecruitableAsync(Nation nation, EventId id)
        {
            if (nation != null)
            {
                var result = await IsNationRecruitableAsync(nation.Name, id);
                if (result == 0)
                {
                    await NationManager.SetNationStatusToAsync(nation, "skipped");
                }
                else if (result == 2)
                {
                    await NationManager.SetNationStatusToAsync(nation, "failed");
                }
                else
                {
                    return true;
                }
            }
            return false;
        }

        private async Task<int> IsNationRecruitableAsync(string nationName, EventId id)
        {
            if (!string.IsNullOrWhiteSpace(nationName))
            {
                var criteriaFit = await DoesNationFitCriteriaAsync(nationName);
                if (criteriaFit)
                {
                    var apiResponse = await WouldReceiveTelegramAsync(nationName);
                    if (apiResponse == 0)
                    {
                        _logger.LogDebug(id, LogMessageBuilder.Build(id, $"{nationName} wouldn't receive a telegram and is therefore skipped."));
                        return 0;
                    }
                    else if (apiResponse == 1)
                    {
                        return 1;
                    }
                    else
                    {
                        _logger.LogDebug(id, LogMessageBuilder.Build(id, $"Recruitable nation check: {nationName} failed."));
                        return 2;
                    }
                }
                else
                {
                    _logger.LogDebug(id, LogMessageBuilder.Build(id, $"{nationName} does not fit criteria and is therefore skipped."));
                    return 0;
                }
            }
            else
            {
                return 0;
            }
        }

        private async Task<bool> DoesNationFitCriteriaAsync(string nationName)
        {
            if (_config.CriteriaCheckOnNations)
            {
                var res = !nationName.Any(c => char.IsDigit(c)) && nationName.Count(c => c == nationName[0]) != nationName.Length;
                res = res && !nationName.Contains("puppet", StringComparison.InvariantCultureIgnoreCase);
                _logger.LogDebug($"{nationName} criteria fit: {res}");
                return await Task.FromResult(res);
            }
            else
            {
                return await Task.FromResult(true);
            }
        }

        public async Task<int> WouldReceiveTelegramAsync(string nationName)
        {
            var id = LogEventIdProvider.GetEventIdByType(LoggingEvent.WouldReceiveTelegram);
            try
            {
                XmlDocument result = await _apiService.GetWouldReceiveTelegramAsync(nationName);
                if (result != null)
                {
                    XmlNodeList canRecruitNodeList = result.GetElementsByTagName("TGCANRECRUIT");
                    return canRecruitNodeList[0].InnerText == "1" ? 1 : 0;
                }
                else
                {
                    _logger.LogWarning(id, LogMessageBuilder.Build(id, $"Result of GetWouldReceiveTelegramAsync '{nationName}' were null. Result 2."));
                    return 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(id, ex, LogMessageBuilder.Build(id, $"GetWouldReceiveTelegramAsync '{nationName}' failed. Result 3."));
                return 2;
            }
        }

        private async Task GetNewNationsAsync()
        {
            var id = LogEventIdProvider.GetEventIdByType(LoggingEvent.GetNewNations);
            while (IsRecruiting)
            {
                try
                {
                    await _apiService.WaitForActionAsync(NationStatesApiRequestType.GetNewNations);
                    PoolStatus = "Filling up with new nations";
                    var result = await _apiService.GetNewNationsAsync(id);
                    await AddNationToPendingAsync(id, result, false);
                    PoolStatus = "Waiting for new nations";
                }
                catch (Exception ex)
                {
                    _logger.LogError(id, ex, LogMessageBuilder.Build(id, "An error occured."));
                }
                await Task.Delay(300000);
            }
        }

        private async Task EnsurePoolFilledAsync()
        {
            var id = LogEventIdProvider.GetEventIdByType(LoggingEvent.EnsurePoolFilled);
            List<REGION> regionsToRecruitFrom = await GetRegionToRecruitFromAsync(id);
            while (IsRecruiting)
            {
                bool fillingUp = false;
                int counter = 0;
                int pendingCount = NationManager.GetNationCountByStatusName("pending");
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
                    while (await NationManager.IsNationPendingSkippedSendOrFailedAsync(nationName) || await IsNationRecruitableAsync(nationName, id) != 1);
                    var nation = await NationManager.GetNationAsync(nationName);
                    if (nation != null)
                    {
                        await NationManager.SetNationStatusToAsync(nation, "pending");
                    }
                    else
                    {
                        await NationManager.AddUnknownNationsAsPendingAsync(new List<string>() { nationName }, true);
                    }
                    counter++;
                    pendingCount = NationManager.GetNationCountByStatusName("pending");
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

        private async Task<List<REGION>> GetRegionToRecruitFromAsync(EventId id)
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

        private async Task AddNationToPendingAsync(EventId id, List<string> nationNames, bool isSourceDumps)
        {
            List<string> nationsToAdd = new List<string>();
            foreach (var res in nationNames)
            {
                if (await IsNationRecruitableAsync(res, id) == 1)
                {
                    nationsToAdd.Add(res);
                }
            }
            var counter = await NationManager.AddUnknownNationsAsPendingAsync(nationsToAdd, isSourceDumps);
            if (counter > 0)
            {
                _logger.LogInformation(id, LogMessageBuilder.Build(id, $"{counter} nations added to pending"));
            }
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
                        if (NationManager.GetNationCountByStatusName("pending") == 0)
                        {
                            _logger.LogWarning("Delaying API recruitment for 15 minutes due to lack of recruitable nations");
                            RecruitmentStatus = "Throttled: lack of nations";
                            await Task.Delay(900000);
                        }
                        pendingNations = NationManager.GetNationsByStatusName("reserved_api");
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
                            if (await IsNationRecruitableAsync(nation, _defaulEventId))
                            {
                                if (await _apiService.SendRecruitmentTelegramAsync(nation.Name))
                                {
                                    await NationManager.SetNationStatusToAsync(nation, "send");
                                    _logger.LogInformation(_defaulEventId, LogMessageBuilder.Build(_defaulEventId, $"Telegram to {nation.Name} queued successfully."));
                                }
                                else
                                {
                                    _logger.LogWarning(_defaulEventId, LogMessageBuilder.Build(_defaulEventId, $"Sending of a Telegram to {nation.Name} failed."));
                                    await NationManager.SetNationStatusToAsync(nation, "failed");
                                }
                            }
                            pendingNations.Remove(nation);
                        }
                    }
                    else
                    {
                        _logger.LogCritical(_defaulEventId, LogMessageBuilder.Build(_defaulEventId, "No nation to recruit found."));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(_defaulEventId, ex, LogMessageBuilder.Build(_defaulEventId, "An error occured."));
                }
                await Task.Delay(60000);
            }
            if (!_config.EnableRecruitment)
            {
                _logger.LogWarning(_defaulEventId, "Recruitment disabled.");
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

        public async Task UpdateRecruitmentStatsAsync()
        {
            try
            {
                _logger.LogInformation(_defaulEventId, LogMessageBuilder.Build(_defaulEventId, "Updating Recruitment Stats"));
                var today = DateTime.Today.Date;

                var sent = NationManager.GetNationsByStatusName("send").Select(n => n.Name).ToList();
                var manual = NationManager.GetNationsByStatusName("reserved_manual").Select(n => n.Name).ToList();
                var region = await _dumpDataService.GetRegionAsync(BaseApiService.ToID(_config.NationStatesRegionName));

                var apiRecruited = region.NATIONS.Where(n => sent.Any(s => n.NAME == s)).Select(n => n.NAME).ToList();
                var manualRecruited = region.NATIONS.Where(n => manual.Any(m => n.NAME == m)).Select(n => n.NAME).ToList();

                RStatDbUpdate();

                ApiRecruited = apiRecruited.Count;
                ApiRatio = Math.Round((100 * ApiRecruited / (sent.Count + ApiFailed + 0.0)), 2);
                ManualReserved = manual.Count;
                ManualRecruited = manualRecruited.Count;
                ManualRatio = Math.Round((100 * ManualRecruited / (manual.Count + 0.0)), 2);
                _logger.LogInformation(_defaulEventId, LogMessageBuilder.Build(_defaulEventId, "Recruitment Stats Updated"));
            }
            catch (Exception ex)
            {
                _logger.LogCritical(_defaulEventId, ex, LogMessageBuilder.Build(_defaulEventId, "A critical error occured."));
            }
        }

        private void RStatDbUpdate()
        {
            ApiSent = NationManager.GetNationsByStatusName("send").Count;
            ApiPending = NationManager.GetNationsByStatusName("pending").Count;
            ApiSkipped = NationManager.GetNationsByStatusName("skipped").Count;
            ApiFailed = NationManager.GetNationsByStatusName("failed").Count;
        }
    }
}