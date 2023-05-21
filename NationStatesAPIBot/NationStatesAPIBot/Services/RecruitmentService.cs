using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NationStatesAPIBot.Commands.Management;
using NationStatesAPIBot.DumpData;
using NationStatesAPIBot.Entities;
using NationStatesAPIBot.Manager;
using NationStatesAPIBot.Types;

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
        private readonly string _romanNumberFilter = "^M{0,4}(CM|CD|D?C{0,3})(XC|XL|L?X{0,3})(IX|IV|V?I{0,3})$";
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

        public bool IsReceivingRecruitableNations { get; private set; }
        public static bool IsRecruiting { get; private set; }
        public static string RecruitmentStatus { get; private set; } = "Not Running";
        public static string PoolStatus { get; private set; } = "Waiting for new nations";

        public void StartRecruitment()
        {
            if (!IsRecruiting)
            {
                IsRecruiting = true;

                //Task.Run(async () => await EnsurePoolFilledAsync());
                Task.Run(async () => await RecruitAsync());
                RecruitmentStatus = "Started";
                //Task.Run(async () => await UpdateRecruitmentStatsAsync());
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

        public async IAsyncEnumerable<Nation> GetRecruitableNationsAsync(int number, bool isAPI, EventId id)
        {
            _logger.LogInformation(id, LogMessageBuilder.Build(id, $"{number} recruitable nations requested"));
            var pendingCount = NationManager.GetNationCountByStatusName("pending");
            _logger.LogInformation(id, LogMessageBuilder.Build(id, $"Pending: {pendingCount}"));
            if (number > pendingCount)
            {
                number = pendingCount;
            }
            for (int i = 0; i < number; i++)
            {
                if (currentRNStatus != null && currentRNStatus.CurrentCount > currentRNStatus.FinalCount)
                {
                    break;
                }
                if (IsReceivingRecruitableNations && !isAPI)
                {
                    currentRNStatus.CurrentCount++;
                }
                var nation = await GetRecruitableNationAsync(id, isAPI);
                if (nation == null)
                {
                    break;
                }
                yield return nation;
            }
        }

        public async Task<Nation> GetRecruitableNationAsync(EventId id, bool isAPI)
        {
            bool found = false;
            Nation result = null;
            do
            {
                var nation = await NationManager.GetNationByStatusNameAsync("pending");
                if (nation != null)
                {
                    found = await IsNationRecruitableAsync(nation, id);
                    if (found)
                    {
                        result = nation;
                    }
                    else
                    {
                        if (IsReceivingRecruitableNations && !isAPI)
                        {
                            currentRNStatus.SkippedCount++;
                        }
                    }
                }
                else
                {
                    _logger.LogCritical(id, "Picked nation was null !");
                    break;
                }
            } while (!found);
            return result;
        }

        private async Task<bool> IsNationRecruitableAsync(Nation nation, EventId id)
        {
            if (nation != null)
            {
                var criteriaFit = DoesNationFitCriteria(nation.Name);
                if (criteriaFit)
                {
                    var apiResponse = await WouldReceiveTelegramAsync(nation, id);
                    if (apiResponse == 0)
                    {
                        _logger.LogDebug(id, LogMessageBuilder.Build(id, $"{nation.Name} - No receive."));
                    }
                    else if (apiResponse == 1)
                    {
                        return true;
                    }
                    else
                    {
                        _logger.LogWarning(id, LogMessageBuilder.Build(id, $"Recruitable nation check: {nation.Name} failed."));
                        await NationManager.SetNationStatusToAsync(nation, "failed");
                        return false;
                    }
                }
                else
                {
                    _logger.LogDebug(id, LogMessageBuilder.Build(id, $"{nation.Name} does not fit criteria and is therefore skipped."));
                }
            }
            await NationManager.SetNationStatusToAsync(nation, "skipped");
            return false;
        }

        private bool DoesNationFitCriteria(string nationName)
        {
            if (_config.CriteriaCheckOnNations)
            {
                var isValid = !nationName.Contains("puppet", StringComparison.InvariantCultureIgnoreCase);
                isValid = isValid && !nationName.Contains("founder", StringComparison.InvariantCultureIgnoreCase);
                isValid = isValid && !nationName.Contains("shit", StringComparison.InvariantCultureIgnoreCase);
                isValid = isValid && !nationName.Contains("damn", StringComparison.InvariantCultureIgnoreCase);
                // Catches facis* like facist(s), facism
                isValid = isValid && !nationName.Contains("facis", StringComparison.InvariantCultureIgnoreCase);
                // Catches fascis* like fascist(s), fascism
                isValid = isValid && !nationName.Contains("fascis", StringComparison.InvariantCultureIgnoreCase);
                // Catches fascis* like faschist(en), faschismus
                isValid = isValid && !nationName.Contains("faschis", StringComparison.InvariantCultureIgnoreCase);
                // Catches racis* like racist(s), racism
                isValid = isValid && !nationName.Contains("racis", StringComparison.InvariantCultureIgnoreCase);
                isValid = isValid && !nationName.Contains("homophop", StringComparison.InvariantCultureIgnoreCase);
                isValid = isValid && !nationName.Contains("transphop", StringComparison.InvariantCultureIgnoreCase);
                // Catches maois* like maoist(s), maoism
                isValid = isValid && !nationName.Contains("maois", StringComparison.InvariantCultureIgnoreCase);
                isValid = isValid && !nationName.Contains("nazi", StringComparison.InvariantCultureIgnoreCase);
                isValid = isValid && !nationName.Contains("nationalsocial", StringComparison.InvariantCultureIgnoreCase);
                isValid = isValid && !nationName.Contains("national_social", StringComparison.InvariantCultureIgnoreCase);
                isValid = isValid && !nationName.Contains("national-social", StringComparison.InvariantCultureIgnoreCase);
                isValid = isValid && !nationName.Contains("nationalsozial", StringComparison.InvariantCultureIgnoreCase);
                isValid = isValid && !nationName.Contains("national_sozial", StringComparison.InvariantCultureIgnoreCase);
                isValid = isValid && !nationName.Contains("national-sozial", StringComparison.InvariantCultureIgnoreCase);
                isValid = isValid && !nationName.Contains("hitler", StringComparison.InvariantCultureIgnoreCase);
                isValid = isValid && !nationName.Contains("macht", StringComparison.InvariantCultureIgnoreCase);
                isValid = isValid && !nationName.Contains("wehr", StringComparison.InvariantCultureIgnoreCase);
                // Catches preus* like preussen, preusisch, preusisches etc.
                isValid = isValid && !nationName.Contains("preus", StringComparison.InvariantCultureIgnoreCase);
                // Catches prus* like prussia, prussian etc.
                isValid = isValid && !nationName.Contains("prus", StringComparison.InvariantCultureIgnoreCase);
                isValid = isValid && !nationName.Contains("reich", StringComparison.InvariantCultureIgnoreCase);
                isValid = isValid && !nationName.Contains("arisch", StringComparison.InvariantCultureIgnoreCase);
                isValid = isValid && !nationName.Contains("aryan", StringComparison.InvariantCultureIgnoreCase);
                isValid = isValid && !nationName.Contains("deutsch", StringComparison.InvariantCultureIgnoreCase);
                isValid = isValid && !nationName.Contains("german", StringComparison.InvariantCultureIgnoreCase);
                isValid = !nationName.Any(c => char.IsDigit(c)) && nationName.Count(c => c == nationName[0]) != nationName.Length;
                isValid = isValid && !ContainsRomanNumber(nationName);
                //_logger.LogDebug($"{nationName} criteria fit: {isValid}");
                return isValid;
            }
            else
            {
                return true;
            }
        }

        private bool ContainsRomanNumber(string nationName)
        {
            var parts = nationName.Split("_");
            return parts.Any(n => Regex.IsMatch(n, _romanNumberFilter, RegexOptions.IgnoreCase));
        }

        public async Task<int> WouldReceiveTelegramAsync(Nation nation, EventId id)
        {
            try
            {
                await _dumpDataService.WaitForDataAvailabilityAsync();
                var lastUpdated = DumpDataService.LastDumpUpdateTimeUtc;
                if (nation.StatusTime < new DateTimeOffset(lastUpdated.Year, lastUpdated.Month, lastUpdated.Day, 6, 30, 0, TimeSpan.Zero).UtcDateTime)
                {
                    var exists = await _dumpDataService.DoesNationExistInDumpAsync(nation.Name);
                    if (!exists)
                    {
                        _logger.LogDebug(id, LogMessageBuilder.Build(id, $"Nation {nation.Name} probably CTEd according to Dump. Skipped."));
                        return 0;
                    }
                }

                XmlDocument result = await _apiService.GetWouldReceiveTelegramAsync(nation.Name);
                if (result != null)
                {
                    XmlNodeList canRecruitNodeList = result.GetElementsByTagName("TGCANRECRUIT");
                    return canRecruitNodeList[0].InnerText == "1" ? 1 : 0;
                }
                else
                {
                    _logger.LogDebug(id, LogMessageBuilder.Build(id, $" WouldReceive -> '{nation.Name}' => null. Skipped."));
                    return 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(id, ex, LogMessageBuilder.Build(id, $"GetWouldReceiveTelegramAsync '{nation.Name}' failed."));
                return 2;
            }
        }

        public async Task GetNewNationsAsync()
        {
            while (true)
            {
                var id = LogEventIdProvider.GetEventIdByType(LoggingEvent.GetNewNations);
                try
                {
                    //PoolStatus = "Filling up with new nations";
                    PoolStatus = "Waiting for new nations";
                    var result = await _apiService.GetNewNationsAsync(id);
                    await AddNationToPendingAsync(id, result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(id, ex, LogMessageBuilder.Build(id, "An error occured."));
                }
            }
        }

        public async Task EnsurePoolFilledAsync()
        {
            var id = LogEventIdProvider.GetEventIdByType(LoggingEvent.EnsurePoolFilled);
            List<REGION> regionsToRecruitFrom = await GetRegionToRecruitFromAsync(id);

            while (true)
            {
                int pendingCount = NationManager.GetNationCountByStatusName("pending");
                if (true)
                {
                    try
                    {
                        //PoolStatus = "Filling up with nations from regions to recruit from";
                        var regionId = _rnd.Next(regionsToRecruitFrom.Count);
                        var region = regionsToRecruitFrom.ElementAt(regionId);
                        var candidates = regionsToRecruitFrom.SelectMany(region => region.NATIONS.Where(n => n.FIRSTLOGIN > DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(14)) && !NationManager.IsNationInDb(n.NAME) && DoesNationFitCriteria(n.NAME))).ToList();
                        var wacandidates = candidates.Where(n => n.WAMEMBER);
                        //var recentlyFoundedNations = region.NATIONS.Where(n => n.FIRSTLOGIN > DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(7)) && !NationManager.IsNationInDb(n.NAME) && DoesNationFitCriteria(n.NAME)).ToList();
                        var neededNationsCount = _config.MinimumRecruitmentPoolSize - pendingCount;

                        var potentialRecruitmentTargets = wacandidates.Take(neededNationsCount).ToList();
                        if (potentialRecruitmentTargets.Count < neededNationsCount)
                        {
                            potentialRecruitmentTargets.AddRange(candidates.Take(neededNationsCount - potentialRecruitmentTargets.Count).ToList());
                        }
                        //candidates.Take(neededNationsCount).Select(n => n.NAME).ToList();
                        _logger.LogInformation($"EnsurePoolFilled would have added {potentialRecruitmentTargets.Count} nations to the pool if properly enabled.");
                        //await AddNationToPendingAsync(id, candidates.Select(n => n.NAME).ToList(), false);
                        //pendingCount = NationManager.GetNationCountByStatusName("pending");
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(id, e, "Error:");
                    }
                }
                await Task.Delay(27000000);
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

        private async Task AddNationToPendingAsync(EventId id, List<string> nationNames)
        {
            await AddNationToPendingAsync(id, nationNames, false);
        }

        private async Task AddNationToPendingAsync(EventId id, List<string> nationNames, bool skipCriteriaCheck)
        {
            List<string> nationsToAdd = new List<string>();
            if (!skipCriteriaCheck)
            {
                foreach (var res in nationNames)
                {
                    if (DoesNationFitCriteria(res))
                    {
                        nationsToAdd.Add(res);
                    }
                }
            }
            else
            {
                nationsToAdd = nationNames;
            }
            var counter = await NationManager.AddUnknownNationsAsPendingAsync(nationsToAdd);
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
                    if (NationManager.GetNationCountByStatusName("pending") == 0)
                    {
                        _logger.LogWarning("Delaying API recruitment for 15 minutes due to lack of recruitable nations");
                        RecruitmentStatus = "Throttled: lack of nations";
                        await Task.Delay(900000);
                    }
                    pendingNations = NationManager.GetNationsByStatusName("reserved_api");
                    if (pendingNations.Count == 0)
                    {
                        var numberToRequest = 10;
                        await foreach (var resNation in GetRecruitableNationsAsync(numberToRequest, true, _defaulEventId))
                        {
                            pendingNations.Add(resNation);
                        }
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

                    if (await _apiService.IsNationStatesApiActionReadyAsync(NationStatesApiRequestType.SendRecruitmentTelegram, true))
                    {
                        bool recruitable = false;
                        do
                        {
                            var picked = pendingNations.Take(1);
                            var nation = picked.Count() > 0 ? picked.ToArray()[0] : null;
                            recruitable = await IsNationRecruitableAsync(nation, _defaulEventId);
                            if (nation != null)
                            {
                                if (recruitable)
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
                            else
                            {
                                _logger.LogCritical(_defaulEventId, LogMessageBuilder.Build(_defaulEventId, "No nation to recruit found."));
                            }
                        }
                        while (!recruitable && pendingNations.Count > 0);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(_defaulEventId, ex, LogMessageBuilder.Build(_defaulEventId, "An error occured."));
                }
                await Task.Delay(10000);
            }

            if (!_config.EnableRecruitment)
            {
                _logger.LogWarning(_defaulEventId, "Recruitment disabled.");
            }
        }

        internal string GetRNStatus()
        {
            return IsReceivingRecruitableNations ? currentRNStatus.ToString() : "No /rn command currently running.";
        }

        public Task UpdateRecruitmentStatsAsync()
        {
            try
            {
                _logger.LogInformation(_defaulEventId, LogMessageBuilder.Build(_defaulEventId, "Updating Recruitment Stats"));
                RStatDbUpdate();
                _logger.LogInformation(_defaulEventId, LogMessageBuilder.Build(_defaulEventId, "Recruitment Stats Updated"));
            }
            catch (Exception ex)
            {
                _logger.LogCritical(_defaulEventId, ex, LogMessageBuilder.Build(_defaulEventId, "A critical error occured."));
            }
            return Task.CompletedTask;
        }

        private void RStatDbUpdate()
        {
            ApiSent = NationManager.GetNationCountByStatusName("send");
            ApiPending = NationManager.GetNationCountByStatusName("pending");
            ApiSkipped = NationManager.GetNationCountByStatusName("skipped");
            ApiFailed = NationManager.GetNationCountByStatusName("failed");
            ManualReserved = NationManager.GetNationCountByStatusName("reserved_manual");
        }
    }
}