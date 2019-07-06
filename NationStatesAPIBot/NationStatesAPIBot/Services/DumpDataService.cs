﻿using System;
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
        private readonly CancellationTokenSource tokenSource = new CancellationTokenSource();
        private static DateTime lastDumpUpdateTime = DateTime.UnixEpoch;
        private readonly string regionFileName = "regions-dump-latest.xml.gz";
        private readonly string nationFileName = "nations-dump-latest.xml.gz";
        private readonly EventId defaultEventId;
        public DumpDataService(ILogger<DumpDataService> logger, NationStatesApiService apiService)
        {
            _logger = logger;
            _apiService = apiService;
            defaultEventId = LogEventIdProvider.GetEventIdByType(LoggingEvent.DumpDataServiceAction);
            _logger.LogInformation(defaultEventId, GetLogMessage("--- DumpDataService started ---"));
        }

        public static bool IsUpdating { get; private set; } = false;

        public static bool DataAvailable { get; private set; } = false;
        public static DateTime LastDumpUpdateTime { get => lastDumpUpdateTime; private set => lastDumpUpdateTime = value; }

        private string GetLogMessage(string message)
        {
            return LogMessageBuilder.Build(defaultEventId, message);
        }
        #region Management & Parsing
        public void StartDumpUpdateCycle()
        {
            isDumpUpdateCycleRunning = true;
            Task.Run(async () => await UpdateCycle());
        }

        public void StopDumpUpdateCycle()
        {
            isDumpUpdateCycleRunning = false;
            tokenSource.Cancel();
        }

        private async Task UpdateCycle()
        {
            _logger.LogInformation(defaultEventId, GetLogMessage("--- DumpDataService Update Cycle has been started ---"));
            while (isDumpUpdateCycleRunning)
            {
                await UpdateData();
            }
            _logger.LogWarning(defaultEventId, GetLogMessage("--- DumpDataService Update Cycle has been stopped ---"));
        }

        private bool IsLocalDataAvailable()
        {
            var existence = File.Exists(regionFileName) && File.Exists(nationFileName);
            if (existence)
            {
                var fileInfoRegions = new FileInfo(regionFileName);
                var fileInfoNations = new FileInfo(nationFileName);
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

        private async Task UpdateData()
        {
            try
            {
                if (LastDumpUpdateTime == DateTime.UnixEpoch)
                {
                    await InitialUpdate();
                }
                else
                {
                    await _apiService.WaitForAction(NationStatesApiRequestType.DownloadDumps, TimeSpan.FromMinutes(30), tokenSource.Token);
                    if (!tokenSource.Token.IsCancellationRequested)
                    {
                        IsUpdating = true;
                        _logger.LogInformation(defaultEventId, GetLogMessage("--- Updating NATION and REGION collections from dumps ---"));
                        await DowloadAndReadDumpsAsync();
                    }
                }
                _logger.LogInformation(defaultEventId, GetLogMessage("--- Dump Data Update Finished ---"));
            }
            catch (Exception ex)
            {
                _logger.LogCritical(defaultEventId, ex, GetLogMessage("A critical error occurred while processing of Dump Update"));
            }
            finally
            {
                IsUpdating = false;
            }
        }

        private async Task InitialUpdate()
        {
            _logger.LogInformation("--- Updating NATION and REGION collections from dumps ---");
            IsUpdating = true;
            DataAvailable = false;
            _logger.LogDebug(defaultEventId, GetLogMessage("No Dumpdata available"));
            if (IsLocalDataAvailable())
            {
                _logger.LogDebug(defaultEventId, GetLogMessage("Reading Dumps from local Filesystem"));
                ReadDumpsFromLocalFileSystem();
            }
            else
            {
                _logger.LogDebug(defaultEventId, GetLogMessage("Downloading and Reading Dumps"));
                await DowloadAndReadDumpsAsync();
            }
        }

        private void LoadDumpsFromStream(GZipStream regionsStream, GZipStream nationsStream)
        {
            Stopwatch stopWatch = Stopwatch.StartNew();
            _regions = GetRegionsFromStream(regionsStream);
            stopWatch.Stop();
            _logger.LogDebug(defaultEventId, GetLogMessage($"Parsing regions took {stopWatch.Elapsed} to complete."));
            stopWatch.Restart();
            _nations = GetNationsFromStream(nationsStream);
            stopWatch.Stop();
            _logger.LogDebug(defaultEventId, GetLogMessage($"Parsing nations took {stopWatch.Elapsed} to complete."));
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
                fsr = new FileStream(regionFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                regionStream = new GZipStream(fsr, CompressionMode.Decompress);

                stopWatch.Stop();
                _logger.LogDebug(defaultEventId, GetLogMessage($"Reading region dump from local cache took {stopWatch.Elapsed} to complete."));
                stopWatch.Restart();
                fsn = new FileStream(nationFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                nationStream = new GZipStream(fsn, CompressionMode.Decompress);

                stopWatch.Stop();
                _logger.LogDebug(defaultEventId, GetLogMessage($"Reading nation dump from local cache took {stopWatch.Elapsed} to complete."));
                var fileInfoNations = new FileInfo(nationFileName);
                LastDumpUpdateTime = fileInfoNations.CreationTimeUtc;
                LoadDumpsFromStream(regionStream, nationStream);
            }
            finally
            {
                fsr?.Close();
                fsn?.Close();
                nationStream?.Close();
                regionStream?.Close();
                fsr.Close();
                fsn.Close();
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
                regionsStream = await _apiService.GetNationStatesDumpStream(NationStatesDumpType.Regions);
                stopWatch.Stop();
                _logger.LogDebug(defaultEventId, GetLogMessage($"Download region dump as stream took {stopWatch.Elapsed} to complete."));
                stopWatch.Restart();
                nationsStream = await _apiService.GetNationStatesDumpStream(NationStatesDumpType.Nations);
                stopWatch.Stop();
                _logger.LogDebug(defaultEventId, GetLogMessage($"Download nation dump as stream took {stopWatch.Elapsed} to complete."));
                stopWatch.Restart();
                await WriteDumpToLocalFileSystemAsync(NationStatesDumpType.Regions, regionsStream);
                stopWatch.Stop();
                _logger.LogDebug(defaultEventId, GetLogMessage($"Writing region dump to local cache took {stopWatch.Elapsed} to complete."));
                stopWatch.Restart();
                await WriteDumpToLocalFileSystemAsync(NationStatesDumpType.Nations, nationsStream);
                stopWatch.Stop();
                _logger.LogDebug(defaultEventId, GetLogMessage($"Writing nation dump from local cache took {stopWatch.Elapsed} to complete."));
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
            string fileName = dumpType == NationStatesDumpType.Nations ? nationFileName : regionFileName;
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
            _logger.LogDebug(defaultEventId, GetLogMessage("Extracting stream to REGION Collection"));
            var result = ParseRegionsFromStream(stream);
            _logger.LogDebug(defaultEventId, GetLogMessage("REGION Collection extracted successfully."));
            return result;
        }

        private HashSet<NATION> GetNationsFromStream(Stream stream)
        {
            _logger.LogDebug(defaultEventId, GetLogMessage("Extracting stream to NATION Collection"));
            var result = ParseNationsFromStream(stream);
            _logger.LogDebug(defaultEventId, GetLogMessage("NATION Collection extracted successfully."));
            return result;
        }

        private HashSet<REGION> ParseRegionsFromStream(Stream stream)
        {
            var xml = XDocument.Load(stream, LoadOptions.None);
            return xml.Descendants("REGION").Select(m =>

                new REGION
                {
                    NAME = BaseApiService.ToID(m.Element("NAME").Value),
                    DumpPosition = m.NodesBeforeSelf().Count(),
                    NUMNATIONS = (int)m.Element("NUMNATIONS"),
                    NATIONNAMES = m.Element("NATIONS").Value.Split(":").ToHashSet(),
                    DELEGATE = m.Element("DELEGATE").Value,
                    DELEGATEVOTES = (int)m.Element("DELEGATEVOTES"),
                    DELEGATEAUTH = m.Element("DELEGATEAUTH").Value,
                    FOUNDER = m.Element("FOUNDER").Value,
                    FOUNDERAUTH = m.Element("FOUNDERAUTH").Value,
                    POWER = m.Element("POWER").Value,
                    FLAG = m.Element("FLAG").Value,
                    LASTUPDATE = DateTimeOffset.FromUnixTimeSeconds((int)m.Element("LASTUPDATE")),
                    OFFICERS = BuildOfficers(m),
                    EMBASSIES = m.Element("EMBASSIES").Descendants("EMBASSY").Select(e => e.Value).ToList(),
                    WABADGES = BuildWABadges(m)
                }).ToHashSet();
        }

        private List<OFFICER> BuildOfficers(XElement m)
        {
            return m.Element("OFFICERS").Descendants("OFFICER").Select(o => new OFFICER
            {
                NATION = o.Element("NATION").Value,
                BY = o.Element("BY").Value,
                OFFICE = o.Element("OFFICE").Value,
                ORDER = (int)o.Element("ORDER"),
                AUTHORITY = o.Element("AUTHORITY").Value,
                TIME = DateTimeOffset.FromUnixTimeSeconds((long)o.Element("TIME")),
            }).ToList();
        }

        private HashSet<NATION> ParseNationsFromStream(Stream stream)
        {
            HashSet<NATION> nations = new HashSet<NATION>();

            XmlReader reader = XmlReader.Create(stream);
            reader.ReadToDescendant("NATIONS");
            reader.ReadToDescendant("NATION");
            do
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(reader.ReadOuterXml());
                XElement nation = XElement.Load(doc.DocumentElement.CreateNavigator().ReadSubtree());  // convert xmlnode to xelement
                nations.Add(BuildNation(nation));
            }
            while (reader.ReadToNextSibling("NATION"));
            reader.Close();
            reader.Dispose();
            return nations;
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
                FIRSTLOGIN = DateTimeOffset.FromUnixTimeSeconds((int)m.Element("FIRSTLOGIN")),
                LASTLOGIN = DateTimeOffset.FromUnixTimeSeconds((int)m.Element("LASTLOGIN")),
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
                CARDCATEGORY = m.Element("CARDCATEGORY").Value,
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

        private async Task WaitForDataAvailabilityAsync()
        {
            if (DataAvailable && !IsUpdating)
                return;
            if (IsUpdating)
            {
                while (IsUpdating && !tokenSource.Token.IsCancellationRequested)
                {
                    await Task.Delay(1000);
                }
                tokenSource.Token.ThrowIfCancellationRequested();
            }
            else if (!DataAvailable)
            {
                throw new DataUnavailableException("No data available that could be accessed.");
            }
        }
#endregion
        private REGION GetRegionInternal(string name)
        {
            return _regions.FirstOrDefault(r => r.NAME == name);
        }

        private NATION GetNationInternal(string name)
        {
            return _nations.FirstOrDefault(n => n.NAME == name);
        }

        public async Task<NATION> GetNationAsync(string name)
        {
            _logger.LogDebug(defaultEventId, GetLogMessage($"Dump Data for Nation {name} requested."));
            await WaitForDataAvailabilityAsync();
            return GetNationInternal(name);
        }

        public async Task<REGION> GetRegionAsync(string name)
        {
            _logger.LogDebug(defaultEventId, GetLogMessage($"Dump Data for Region {name} requested."));
            await WaitForDataAvailabilityAsync();
            return GetRegionInternal(name);
        }

        public async Task<List<NATION>> GetWAOfRegion(string regionName)
        {
            _logger.LogDebug(defaultEventId, GetLogMessage($"Dump Data: WA nations of region {regionName} requested."));
            await WaitForDataAvailabilityAsync();
            var region = GetRegionInternal(regionName);
            if (region != null)
            {
                return region.NATIONS.Where(n => n.WAMEMBER).ToList();
            }
            else
            {
                _logger.LogWarning(defaultEventId, GetLogMessage($"region {regionName} was null"));
                return null;
            }
        }

        /// <summary>
        /// Returns a List of Nations that were endorsed by a specific nation
        /// </summary>
        /// <param name="nationName">specific nation name</param>
        /// <returns>List of Nations</returns>
        public async Task<List<NATION>> GetNationsEndorsedBy(string nationName)
        {
            _logger.LogDebug(defaultEventId, GetLogMessage($"Dump Data: GetNationsEndorsedBy {nationName} requested."));
            await WaitForDataAvailabilityAsync();
            var nation = GetNationInternal(nationName);
            if(nation != null)
            {
                var region = nation.REGION;
                if(region != null)
                {
                    return region.NATIONS.Where(n => n.ENDORSEMENTS.Contains(BaseApiService.ToID(nationName))).ToList();
                }
                else
                {
                    _logger.LogWarning(defaultEventId, GetLogMessage($"region of {nation} was null"));
                    return null;
                }
            }
            else
            {
                _logger.LogWarning(defaultEventId, GetLogMessage($"nation {nationName} was null"));
                return null;
            }
        }

    }
}
