using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using NationStatesAPIBot.DumpData;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NationStatesAPIBot.Types;
using System.IO;
using System.Diagnostics;

namespace NationStatesAPIBot.Services
{
    public class DumpDataService
    {
        private readonly ILogger<DumpDataService> _logger;
        private readonly NationStatesApiService _apiService;
        private HashSet<NATION> _nations;
        private HashSet<REGION> _regions;
        private bool isDumpUpdateCycleRunning = false;
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();
        private readonly string _regionFileName = "regions-dump-latest.xml.gz";
        private readonly string _nationFileName = "nations-dump-latest.xml.gz";
        private readonly EventId _defaultEventId;
        private readonly AppSettings _appconf;

        public DumpDataService(ILogger<DumpDataService> logger, NationStatesApiService apiService, IOptions<AppSettings> config)
        {
            _logger = logger;
            _apiService = apiService;
            _appconf = config.Value;
            _defaultEventId = LogEventIdProvider.GetEventIdByType(LoggingEvent.DumpDataServiceAction);
            _logger.LogInformation(_defaultEventId, GetLogMessage("--- DumpDataService started ---"));
        }

        public static bool IsUpdating { get; private set; } = false;

        public static bool DataAvailable { get; private set; } = false;
        public static DateTime LastDumpUpdateTimeUtc { get; private set; } = DateTime.UnixEpoch;

        public static TimeSpan NextDumpDataUpdate
        {
            get
            {
                var next = LastDumpUpdateTimeUtc.Add(new TimeSpan(1, 0, 0, 0, 0));
                if (next.TimeOfDay.Hours > 7 || (next.TimeOfDay.Hours == 7 && next.TimeOfDay.Minutes > 31))
                {
                    next = new DateTime(next.Year, next.Month, next.Day, 7, 31, 0);
                }
                var timespan = next.Subtract(DateTime.UtcNow);
                return timespan;
            }
        }

        private string GetLogMessage(string message)
        {
            return LogMessageBuilder.Build(_defaultEventId, message);
        }

        #region Management & Parsing

        public void StartDumpUpdateCycle()
        {
            isDumpUpdateCycleRunning = true;
            Task.Run(async () => await UpdateCycleAsync());
        }

        public void StopDumpUpdateCycle()
        {
            isDumpUpdateCycleRunning = false;
            _tokenSource.Cancel();
        }

        private async Task UpdateCycleAsync()
        {
            _logger.LogInformation(_defaultEventId, GetLogMessage("--- DumpDataService Update Cycle has been started ---"));
            while (isDumpUpdateCycleRunning)
            {
                await UpdateDataAsync();
            }
            _logger.LogWarning(_defaultEventId, GetLogMessage("--- DumpDataService Update Cycle has been stopped ---"));
        }

        private bool IsLocalDataAvailable()
        {
            var existence = File.Exists(_regionFileName) && File.Exists(_nationFileName);
            if (existence)
            {
                var fileInfoRegions = new FileInfo(_regionFileName);
                var fileInfoNations = new FileInfo(_nationFileName);
                var outdated = fileInfoNations.LastWriteTimeUtc.Date != DateTime.UtcNow.Date || fileInfoRegions.LastWriteTimeUtc.Date != DateTime.UtcNow.Date;
                if (DateTime.UtcNow.TimeOfDay < new TimeSpan(7, 0, 0) && outdated)
                {
                    outdated = false;
                }
                if (outdated)
                {
                    _logger.LogDebug("Local DumpData found but outdated");
                    return false;
                }
                else
                {
                    _logger.LogDebug("Local DumpData found");
                    return true;
                }
            }
            else
            {
                _logger.LogDebug("No Local DumpData found");
                return false;
            }
        }

        private async Task UpdateDataAsync()
        {
            try
            {
                if (LastDumpUpdateTimeUtc == DateTime.UnixEpoch)
                {
                    await InitialUpdateAsync();
                }
                else
                {
                    await _apiService.WaitForActionAsync(NationStatesApiRequestType.DownloadDumps, TimeSpan.FromMinutes(30), _tokenSource.Token);
                    if (!_tokenSource.Token.IsCancellationRequested)
                    {
                        IsUpdating = true;
                        _logger.LogInformation(_defaultEventId, GetLogMessage("--- Updating NATION and REGION collections from dumps ---"));
                        await DowloadAndReadDumpsAsync();
                    }
                }
                _logger.LogInformation(_defaultEventId, GetLogMessage("--- Dump Data Update Finished ---"));
            }
            catch (Exception ex)
            {
                _logger.LogCritical(_defaultEventId, ex, GetLogMessage("A critical error occurred while processing of Dump Update"));
            }
            finally
            {
                IsUpdating = false;
            }
        }

        private async Task InitialUpdateAsync()
        {
            IsUpdating = true;
            DataAvailable = false;
            if (IsLocalDataAvailable())
            {
                _logger.LogDebug(_defaultEventId, GetLogMessage("Reading Dumps from local Filesystem"));
                ReadDumpsFromLocalFileSystem();
            }
            else
            {
                _logger.LogDebug(_defaultEventId, GetLogMessage("No Dumpdata available"));
                _logger.LogDebug(_defaultEventId, GetLogMessage("Downloading and Reading Dumps"));
                await DowloadAndReadDumpsAsync();
            }
        }

        private void LoadDumpsFromStream(GZipStream regionsStream, GZipStream nationsStream)
        {
            Stopwatch stopWatch = Stopwatch.StartNew();
            _regions = GetRegionsFromStream(regionsStream);
            stopWatch.Stop();
            _logger.LogDebug(_defaultEventId, GetLogMessage($"Parsing regions took {stopWatch.Elapsed} to complete."));
            stopWatch.Restart();
            _nations = GetNationsFromStream(nationsStream);
            stopWatch.Stop();
            _logger.LogDebug(_defaultEventId, GetLogMessage($"Parsing nations took {stopWatch.Elapsed} to complete."));
            DataAvailable = true;
        }

        private void ReadDumpsFromLocalFileSystem()
        {
            GZipStream regionStream = null;
            GZipStream nationStream = null;
            FileStream fsr = null;
            FileStream fsn = null;
            try
            {
                Stopwatch stopWatch = Stopwatch.StartNew();
                fsr = new FileStream(_regionFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                regionStream = new GZipStream(fsr, CompressionMode.Decompress);

                stopWatch.Stop();
                _logger.LogDebug(_defaultEventId, GetLogMessage($"Reading region dump from local cache took {stopWatch.Elapsed} to complete."));
                stopWatch.Restart();
                fsn = new FileStream(_nationFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                nationStream = new GZipStream(fsn, CompressionMode.Decompress);

                stopWatch.Stop();
                _logger.LogDebug(_defaultEventId, GetLogMessage($"Reading nation dump from local cache took {stopWatch.Elapsed} to complete."));
                var fileInfoNations = new FileInfo(_nationFileName);
                LastDumpUpdateTimeUtc = fileInfoNations.LastWriteTimeUtc;
                LoadDumpsFromStream(regionStream, nationStream);
            }
            finally
            {
                fsr?.Close();
                fsn?.Close();
                nationStream?.Close();
                regionStream?.Close();
                fsr?.Dispose();
                fsn?.Dispose();
                nationStream?.Dispose();
                regionStream?.Dispose();
            }
        }

        private async Task DowloadAndReadDumpsAsync()
        {
            GZipStream regionsStream = null;
            GZipStream nationsStream = null;
            try
            {
                Stopwatch stopWatch = Stopwatch.StartNew();
                regionsStream = await _apiService.GetNationStatesDumpStreamAsync(NationStatesDumpType.Regions);
                stopWatch.Stop();
                _logger.LogDebug(_defaultEventId, GetLogMessage($"Download region dump as stream took {stopWatch.Elapsed} to complete."));
                stopWatch.Restart();
                nationsStream = await _apiService.GetNationStatesDumpStreamAsync(NationStatesDumpType.Nations);
                stopWatch.Stop();
                _logger.LogDebug(_defaultEventId, GetLogMessage($"Download nation dump as stream took {stopWatch.Elapsed} to complete."));
                stopWatch.Restart();
                await WriteDumpToLocalFileSystemAsync(NationStatesDumpType.Regions, regionsStream);
                stopWatch.Stop();
                _logger.LogDebug(_defaultEventId, GetLogMessage($"Writing region dump to local cache took {stopWatch.Elapsed} to complete."));
                stopWatch.Restart();
                await WriteDumpToLocalFileSystemAsync(NationStatesDumpType.Nations, nationsStream);
                stopWatch.Stop();
                _logger.LogDebug(_defaultEventId, GetLogMessage($"Writing nation dump to local cache took {stopWatch.Elapsed} to complete."));
                ReadDumpsFromLocalFileSystem();
            }
            finally
            {
                nationsStream?.Close();
                regionsStream?.Close();
                nationsStream?.Dispose();
                regionsStream?.Dispose();
            }
        }

        private async Task WriteDumpToLocalFileSystemAsync(NationStatesDumpType dumpType, Stream stream)
        {
            if (!(dumpType == NationStatesDumpType.Nations || dumpType == NationStatesDumpType.Regions))
                throw new ArgumentException("Unknown DumpType");
            string fileName = dumpType == NationStatesDumpType.Nations ? _nationFileName : _regionFileName;
            using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                using (var newCompressed = new GZipStream(fs, CompressionMode.Compress))
                {
                    await stream.CopyToAsync(newCompressed);
                }
            }
        }

        private HashSet<REGION> GetRegionsFromStream(Stream stream)
        {
            _logger.LogDebug(_defaultEventId, GetLogMessage("Extracting stream to REGION Collection"));
            var result = ParseRegionsFromStream(stream);
            _logger.LogDebug(_defaultEventId, GetLogMessage("REGION Collection extracted successfully."));
            return result;
        }

        private HashSet<NATION> GetNationsFromStream(Stream stream)
        {
            _logger.LogDebug(_defaultEventId, GetLogMessage("Extracting stream to NATION Collection"));
            var result = ParseNationsFromStream(stream);
            _logger.LogDebug(_defaultEventId, GetLogMessage("NATION Collection extracted successfully."));
            return result;
        }

        private HashSet<REGION> ParseRegionsFromStream(Stream stream)
        {
            var xml = XDocument.Load(stream, LoadOptions.None);
            return xml.Descendants("REGION").AsParallel().Select(m => {
                var name = BaseApiService.ToID(m.Element("NAME").Value);
                var numnations = (int) m.Element("NUMNATIONS");
                var nationnames = m.Element("NATIONS").Value.Split(":").ToHashSet();
                var delegateVal = m.Element("DELEGATE").Value;
                var delegateVotes = (int) m.Element("DELEGATEVOTES");
                var delegateAuth = m.Element("DELEGATEAUTH").Value;
                var founder = m.Element("FOUNDER").Value;
                var power = m.Element("POWER").Value;
                var flag = m.Element("FLAG").Value;
                var lastUpdate = DateTimeOffset.FromUnixTimeSeconds((int) m.Element("LASTUPDATE"));
                var embassies = m.Element("EMBASSIES").Descendants("EMBASSY").Select(e => e.Value).ToList();
                return new REGION
                {
                    NAME = name,
                    DumpPosition = m.NodesBeforeSelf().Count(),
                    NUMNATIONS = numnations,
                    NATIONNAMES = nationnames,
                    DELEGATE = delegateVal,
                    DELEGATEVOTES = delegateVotes,
                    DELEGATEAUTH = delegateAuth,
                    FOUNDER = founder,
                    FOUNDERAUTH = "- Broken -",
                    POWER = power,
                    FLAG = flag,
                    LASTUPDATE = lastUpdate,
                    OFFICERS = BuildOfficers(m),
                    EMBASSIES = embassies,
                    WABADGES = BuildWABadges(m)
                };
            }).ToHashSet();
        }

        private List<OFFICER> BuildOfficers(XElement m)
        {
            return m.Element("OFFICERS").Descendants("OFFICER").Select(o => new OFFICER
            {
                NATION = o.Element("NATION").Value,
                BY = o.Element("BY").Value,
                OFFICE = o.Element("OFFICE").Value,
                ORDER = (int) o.Element("ORDER"),
                AUTHORITY = o.Element("AUTHORITY").Value,
                TIME = DateTimeOffset.FromUnixTimeSeconds((long) o.Element("TIME")),
            }).ToList();
        }

        private HashSet<NATION> ParseNationsFromStream(Stream stream)
        {
            var xml = XDocument.Load(stream, LoadOptions.None);
            return xml.Descendants("NATION").AsParallel().Select(m => BuildNation(m)).ToHashSet();
        }

        private NATION BuildNation(XElement m)
        {
            return new NATION
            {
                NAME = BaseApiService.ToID(m.Element("NAME").Value),
                TYPE = m.Element("TYPE").Value,
                FULLNAME = m.Element("FULLNAME").Value,
                MOTTO = m.Element("MOTTO").Value,
                CATEGORY = m.Element("CATEGORY").Value,
                WAMEMBER = m.Element("UNSTATUS").Value == "WA Member",
                ENDORSEMENTS = m.Element("ENDORSEMENTS").Value.Split(",").ToList(),
                FREEDOM = BuildFreedom(m),
                REGION = GetRegionInternal(BaseApiService.ToID(m.Element("REGION").Value)),
                REGIONNAME = BaseApiService.ToID(m.Element("REGION").Value),
                POPULATION = Convert.ToDouble(m.Element("POPULATION").Value),
                TAX = Convert.ToDouble(m.Element("TAX").Value),
                ANIMAL = m.Element("ANIMAL").Value,
                CURRENCY = m.Element("CURRENCY").Value,
                DEMONYM = m.Element("DEMONYM").Value,
                DEMONYM2 = m.Element("DEMONYM2").Value,
                DEMONYM2PLURAL = m.Element("DEMONYM2PLURAL").Value,
                FLAG = m.Element("FLAG").Value,
                MAJORINDUSTRY = m.Element("MAJORINDUSTRY").Value,
                GOVTPRIORITY = m.Element("GOVTPRIORITY").Value,
                GOVT = BuildGOVT(m),
                FOUNDED = m.Element("FOUNDED").Value,
                FIRSTLOGIN = DateTimeOffset.FromUnixTimeSeconds((int) m.Element("FIRSTLOGIN")),
                LASTLOGIN = DateTimeOffset.FromUnixTimeSeconds((int) m.Element("LASTLOGIN")),
                LASTACTIVITY = m.Element("LASTACTIVITY").Value,
                INFLUENCE = m.Element("INFLUENCE").Value,
                PUBLICSECTOR = Convert.ToDouble(m.Element("PUBLICSECTOR").Value),
                DEATHS = BuildDeaths(m),
                LEADER = m.Element("LEADER").Value,
                CAPITAL = m.Element("CAPITAL").Value,
                RELIGION = m.Element("RELIGION").Value,
                FACTBOOKS = Convert.ToInt32(m.Element("FACTBOOKS").Value),
                DISPATCHES = Convert.ToInt32(m.Element("DISPATCHES").Value),
                WABADGES = BuildWABadges(m),
                CARDCATEGORY = m.Element("CARDCATEGORY")?.Value,
            };
        }

        private static List<WABADGE> BuildWABadges(XElement m)
        {
            return m.Element("WABADGES")?.Descendants("WABADGE").Select(w => new WABADGE
            {
                Type = w.Attribute("type").Value,
                Value = Convert.ToInt32(w.Value)
            }).ToList();
        }

        private static DEATHS BuildDeaths(XElement m)
        {
            return new DEATHS
            {
                CAUSE = m.Element("DEATHS").Descendants("CAUSE").Select(c => new CAUSE()
                {
                    Type = c.Attribute("type").Value,
                    Value = Convert.ToDouble(c.Value)
                }).ToList()
            };
        }

        private static GOVT BuildGOVT(XElement m)
        {
            return new GOVT
            {
                ADMINISTRATION = Convert.ToDouble(m.Element("GOVT")?.Element("ADMINISTRATION").Value),
                DEFENCE = Convert.ToDouble(m.Element("GOVT")?.Element("DEFENCE").Value),
                EDUCATION = Convert.ToDouble(m.Element("GOVT")?.Element("EDUCATION").Value),
                ENVIRONMENT = Convert.ToDouble(m.Element("GOVT")?.Element("ENVIRONMENT").Value),
                HEALTHCARE = Convert.ToDouble(m.Element("GOVT")?.Element("HEALTHCARE").Value),
                COMMERCE = Convert.ToDouble(m.Element("GOVT")?.Element("COMMERCE").Value),
                INTERNATIONALAID = Convert.ToDouble(m.Element("GOVT")?.Element("INTERNATIONALAID").Value),
                LAWANDORDER = Convert.ToDouble(m.Element("GOVT")?.Element("LAWANDORDER").Value),
                PUBLICTRANSPORT = Convert.ToDouble(m.Element("GOVT")?.Element("PUBLICTRANSPORT").Value),
                SOCIALEQUALITY = Convert.ToDouble(m.Element("GOVT")?.Element("SOCIALEQUALITY").Value),
                SPIRITUALITY = Convert.ToDouble(m.Element("GOVT")?.Element("SPIRITUALITY").Value),
                WELFARE = Convert.ToDouble(m.Element("GOVT")?.Element("WELFARE").Value)
            };
        }

        private static FREEDOM BuildFreedom(XElement m)
        {
            return new FREEDOM
            {
                CIVILRIGHTS = m.Element("FREEDOM")?.Element("CIVILRIGHTS").Value,
                CIVILRIGHTS_SCORE = Convert.ToDouble(m.Element("FREEDOMSCORES")?.Element("CIVILRIGHTS").Value),
                ECONOMY = m.Element("FREEDOM")?.Element("ECONOMY").Value,
                ECONOMY_SCORE = Convert.ToDouble(m.Element("FREEDOMSCORES")?.Element("ECONOMY").Value),
                POLITICALFREEDOM = m.Element("FREEDOM")?.Element("POLITICALFREEDOM").Value,
                POLITICALFREEDOM_SCORE = Convert.ToDouble(m.Element("FREEDOMSCORES")?.Element("POLITICALFREEDOM").Value),
            };
        }

        public async Task WaitForDataAvailabilityAsync()
        {
            if (DataAvailable && !IsUpdating)
            {
                return;
            }
            if (IsUpdating)
            {
                while (IsUpdating && !_tokenSource.Token.IsCancellationRequested)
                {
                    await Task.Delay(500, _tokenSource.Token);
                }
            }
            else if (!DataAvailable)
            {
                throw new DataUnavailableException("No data available that could be accessed.");
            }
        }

        #endregion Management & Parsing

        private REGION GetRegionInternal(string name)
        {
            return _regions.FirstOrDefault(r => r.NAME == name);
        }

        private NATION GetNationInternal(string name)
        {
            return _nations.FirstOrDefault(n => n.NAME == name);
        }

        public async Task<bool> DoesNationExistInDumpAsync(string name)
        {
            await WaitForDataAvailabilityAsync();
            return _nations.Any(n => n.NAME == name);
        }

        public async Task<NATION> GetNationAsync(string name)
        {
            _logger.LogDebug(_defaultEventId, GetLogMessage($"Dump Data for Nation {name} requested."));
            await WaitForDataAvailabilityAsync();
            return GetNationInternal(name);
        }

        public async Task<REGION> GetRegionAsync(string name)
        {
            _logger.LogDebug(_defaultEventId, GetLogMessage($"Dump Data for Region {name} requested."));
            await WaitForDataAvailabilityAsync();
            return GetRegionInternal(name);
        }

        public async Task<List<NATION>> GetWAOfRegionAsync(string regionName)
        {
            _logger.LogDebug(_defaultEventId, GetLogMessage($"Dump Data: WA nations of region {regionName} requested."));
            await WaitForDataAvailabilityAsync();
            var region = GetRegionInternal(regionName);
            if (region != null)
            {
                return region.WANATIONS.ToList();
            }
            else
            {
                _logger.LogWarning(_defaultEventId, GetLogMessage($"region {regionName} was null"));
                return null;
            }
        }

        public async Task<IEnumerable<NATION>> GetAllWaAsync()
        {
            return await Task.FromResult(_nations.Where(n => n.WAMEMBER));
        }

        /// <summary>
        /// Returns a List of Nations that were endorsed by a specific nation
        /// </summary>
        /// <param name="nationName">specific nation name</param>
        /// <returns>List of Nations</returns>
        public async Task<List<NATION>> GetNationsEndorsedByAsync(string nationName)
        {
            _logger.LogDebug(_defaultEventId, GetLogMessage($"Dump Data: GetNationsEndorsedBy {nationName} requested."));
            await WaitForDataAvailabilityAsync();
            var nation = GetNationInternal(BaseApiService.ToID(nationName));
            if (nation != null)
            {
                if (!nation.WAMEMBER)
                {
                    throw new InvalidOperationException("Not a WA Member.");
                }
                var region = nation.REGION;
                if (region != null && region.NAME == BaseApiService.ToID(_appconf.NationStatesRegionName))
                {
                    return region.NATIONS.Where(n => n.ENDORSEMENTS.Contains(BaseApiService.ToID(nationName))).ToList();
                }
                else if (region == null)
                {
                    _logger.LogWarning(_defaultEventId, GetLogMessage($"region of {nation} was null"));
                    return null;
                }
                else
                {
                    throw new InvalidOperationException($"This nation does not reside in {_appconf.NationStatesRegionName}");
                }
            }
            else
            {
                _logger.LogWarning(_defaultEventId, GetLogMessage($"nation {nationName} was null"));
                return null;
            }
        }

        public async Task<List<NATION>> GetNationsNotEndorsedByAsync(string nationName)
        {
            _logger.LogDebug(_defaultEventId, GetLogMessage($"Dump Data: GetNationsNotEndorsedBy {nationName} requested."));
            await WaitForDataAvailabilityAsync();
            var nation = GetNationInternal(BaseApiService.ToID(nationName));
            if (nation != null)
            {
                if (!nation.WAMEMBER)
                {
                    throw new InvalidOperationException("Not a WA Member.");
                }
                var region = nation.REGION;
                if (region != null && region.NAME == BaseApiService.ToID(_appconf.NationStatesRegionName))
                {
                    var list = region.WANATIONS.Where(n => !n.ENDORSEMENTS.Contains(BaseApiService.ToID(nationName))).ToList();
                    list.Remove(nation);
                    return list;
                }
                else if (region == null)
                {
                    _logger.LogWarning(_defaultEventId, GetLogMessage($"region of {nation} was null"));
                    return null;
                }
                else
                {
                    throw new InvalidOperationException($"This nation does not reside in {_appconf.NationStatesRegionName}");
                }
            }
            else
            {
                _logger.LogWarning(_defaultEventId, GetLogMessage($"nation {nationName} was null"));
                return null;
            }
        }

        public async Task<List<NATION>> GetNationsWhoDidNotEndorseNationAsync(string nationName)
        {
            _logger.LogDebug(_defaultEventId, GetLogMessage($"Dump Data: GetNationsWhoDidNotEndorseNation {nationName} requested."));
            await WaitForDataAvailabilityAsync();
            var nation = GetNationInternal(BaseApiService.ToID(nationName));
            if (nation != null)
            {
                if (!nation.WAMEMBER)
                {
                    throw new InvalidOperationException("Not a WA Member.");
                }
                var region = nation.REGION;
                if (region != null && region.NAME == BaseApiService.ToID(_appconf.NationStatesRegionName))
                {
                    return region.WANATIONS.Except(region.NATIONS.Where(n => nation.ENDORSEMENTS.Contains(n.NAME))).ToList();
                }
                else if (region == null)
                {
                    _logger.LogWarning(_defaultEventId, GetLogMessage($"region of {nation} was null"));
                    return null;
                }
                else
                {
                    throw new InvalidOperationException($"This nation does not reside in {_appconf.NationStatesRegionName}");
                }
            }
            else
            {
                _logger.LogWarning(_defaultEventId, GetLogMessage($"nation {nationName} was null"));
                return null;
            }
        }
    }
}