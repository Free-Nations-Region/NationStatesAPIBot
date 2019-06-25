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
        private readonly EventId defaulEventId;
        private readonly AppSettings _config;
        private readonly NationStatesApiService _apiService;
        private readonly DumpDataService _dumpDataService;
        
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
            defaulEventId = LogEventIdProvider.GetEventIdByType(LoggingEvent.APIRecruitment);
        }

        public bool IsReceivingRecruitableNations { get; internal set; }
        public static bool IsRecruiting { get; private set; }

        public void StartRecruitment()
        {
            IsRecruiting = true;
            Task.Run(async () => await GetNewNationsAsync());
            Task.Run(async () => await RecruitAsync());
            UpdateRecruitmentStatsAsync();
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
                    if (await IsNationRecruitableAsync(nation, id))
                    {
                        returnNations.Add(nation);
                        if (IsReceivingRecruitableNations)
                        {
                            currentRNStatus.CurrentCount++;
                        }
                    }
                    pendingNations.Remove(nation);
                    returnNations = returnNations.Distinct().ToList();
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
                if (!await DoesNationFitCriteriaAsync(nation) || !await WouldReceiveTelegram(nation.Name))
                {
                    _logger.LogDebug(id, LogMessageBuilder.Build(id, $"{nation.Name} does not fit criteria and is therefore skipped"));
                    await NationManager.SetNationStatusToAsync(nation, "skipped");
                    return false;
                }
                return true;
            }
            return false;
        }

        private async Task<bool> DoesNationFitCriteriaAsync(Nation nation)
        {
            if (_config.CriteriaCheckOnNations)
            {
                var res = !nation.Name.Any(c => char.IsDigit(c)) && nation.Name.Count(c => c == nation.Name[0]) != nation.Name.Length;
                _logger.LogDebug($"{nation.Name} criteria fit: {res}");
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
                            if (await IsNationRecruitableAsync(nation, defaulEventId))
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
                            }
                            pendingNations.Remove(nation);
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

        private async void UpdateRecruitmentStatsAsync()
        {
            while (IsRecruiting)
            {                
                var today = DateTime.Today.Date;
                
                var sent = NationManager.GetNationsByStatusName("send");
                var manual = NationManager.GetNationsByStatusName("reserved_manual");
                var region = await _dumpDataService.GetRegionAsync(_config.NationStatesRegionName);

                var apiRecruited = region.NATIONS.Where(n => sent.Any(s => n.NAME == s.Name)).ToList();
                var manualRecruited = region.NATIONS.Where(n => manual.Any(m => n.NAME == m.Name)).ToList();
                
                ApiSent = sent.Count;
                ApiPending = NationManager.GetNationsByStatusName("pending").Count;
                ApiSkipped = NationManager.GetNationsByStatusName("skipped").Count;
                ApiFailed = NationManager.GetNationsByStatusName("failed").Count;
                ApiRecruited = apiRecruited.Count;
                ApiRatio = Math.Round((100 * ApiRecruited / (sent.Count + ApiFailed + 0.0)), 2);
                ManualReserved = manual.Count;
                ManualRecruited = manualRecruited.Count;
                ManualRatio = Math.Round((100 * ManualRecruited / (manual.Count + 0.0)), 2);
                
                await WriteRecruited(today, apiRecruited, manualRecruited);
                
                var rt = await GetRecruitedOn(today);
                RecruitedTodayA = rt[0];
                RecruitedTodayM = rt[1];

                var ry = await GetRecruitedOn(today.AddDays(-1));
                RecruitedYesterdayA = ry[0];
                RecruitedYesterdayM = ry[1];

                var lastMonday = (today - new TimeSpan((int) today.DayOfWeek, 0, 0, 0)).AddDays(1);
                var rlw = await GetRecruitedBetween(lastMonday - TimeSpan.FromDays(7), lastMonday);
                RecruitedLastWeekA = rlw[0];
                RecruitedLastWeekM = rlw[1];
                RecruitedLastWeekAvgDA = Math.Round((RecruitedLastWeekA / 7.0), 2);
                RecruitedLastWeekAvgDM = Math.Round((RecruitedLastWeekM / 7.0), 2);
                              
                var firstDayLastMonth = new DateTime(today.Year, today.Month, 1).AddMonths(-1);
                var daysInLastMonth = DateTime.DaysInMonth(firstDayLastMonth.Year, firstDayLastMonth.Month);
                var rlm = await GetRecruitedBetween(firstDayLastMonth, firstDayLastMonth.AddMonths(1).AddDays(-1));
                RecruitedLastMonthA = rlm[0];
                RecruitedLastMonthM = rlm[1];
                RecruitedLastMonthAvgDA = Math.Round(RecruitedLastMonthA / (daysInLastMonth + 0.0), 2);
                RecruitedLastMonthAvgDM = Math.Round(RecruitedLastMonthM / (daysInLastMonth + 0.0), 2);
                
                await Task.Delay(TimeSpan.FromHours(4));
            }
        }
        
        // TODO: Put data in DB
        private async Task WriteRecruited(DateTime date, List<NATION> allApi, List<NATION> allManual)
        {
            string json;
            if (!File.Exists(@"/RecruitmentStats.json"))
            {
                File.Create(@"/RecruitmentStats.json");
                var initial = new JObject(new JProperty("allRemainingRecruits", new JObject(
                    new JProperty("api", allApi), 
                    new JProperty("manual", allManual))));
                json = initial.ToString();
            }
            else
            {
                var stats = JObject.Parse(File.ReadAllText(@"/RecruitmentStats.json"));
                var oldAllApi = stats["all"]["api"].Value<List<NATION>>();
                var oldAllManual = stats["all"]["manual"].Value<List<NATION>>();
                var newApi = allApi.Except(oldAllApi).ToList();
                var newManual = allManual.Except(oldAllManual).ToList();
                stats["all"] = new JObject(new JProperty("api", allApi), new JProperty("manual", allManual));
                var jo = new JObject(new JProperty("api", newApi), new JProperty("manual", newManual));
                if (stats.GetValue($"{date.Date}") == null)
                {
                    stats.Add(new JObject(new JProperty($"{date.Date}", jo)));
                }
                else
                {
                    stats[$"{date.Date}"] = jo;
                }
                json = stats.ToString();
            }
            File.WriteAllText(@"/RecruitmentStats.json", json);
        }
        
        // TODO: Get data from DB
        private async Task<List<int>> GetRecruitedOn(DateTime date)
        {
            var stats = JObject.Parse(File.ReadAllText(@"/RecruitmentStats.json"));
            var checkApi = stats.GetValue("all")["api"].Value<List<NATION>>();
            var checkManual = stats.GetValue("all")["manual"].Value<List<NATION>>();
            var api = stats.GetValue($"{date.Date}")?["api"].Value<List<NATION>>();
            var manual = stats.GetValue($"{date.Date}")?["manual"].Value<List<NATION>>();
            var recruited = new List<int>
            {
                api?.Count(n => checkApi.Any(c => n.NAME == c.NAME)) ?? 0,
                manual?.Count(n => checkManual.Any(c => n.NAME == c.NAME)) ?? 0
            };
            return recruited;
        }
        
        private async Task<List<int>> GetRecruitedBetween(DateTime date1, DateTime date2)
        {
            var days = date2.Subtract(date1).Days;
            var recruited = new List<int>();
            recruited[0] = 0;
            recruited[1] = 0;
            
            for (int i = 0; i < days; i++)
            {
                var ro = await GetRecruitedOn(date1.AddDays(i));
                recruited[0] += ro[0];
                recruited[1] += ro[1];
            }

            return recruited;
        }
    }
}
