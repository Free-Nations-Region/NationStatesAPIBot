using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using DumpData;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NationStatesAPIBot.Types;

namespace NationStatesAPIBot.Services
{
    class DumpDataService
    {
        private readonly BaseApiService _apiService;
        private static List<NATION> _nations;
        private static List<REGION> _regions;
        private static Task _updating;

        public DumpDataService(ILogger<RecruitmentService> logger, IOptions<AppSettings> appSettings, BaseApiService apiService, CancellationToken cancel)
        {
            _apiService = apiService;
            PeriodicUpdate(TimeSpan.FromDays(1), cancel);
        }
        
        private async Task<List<NATION>> GetNationsAsync()
        {
            await Task.WhenAll(_updating);
            return _nations;
        }
        
        private async Task<List<REGION>> GetRegionsAsync()
        {
            await Task.WhenAll(_updating);
            return _regions;
        }
        
        public async Task PeriodicUpdate(TimeSpan interval, CancellationToken cancel)
        {
            while (true)
            {
                await Update();
                await Task.Delay(interval, cancel);
            }
        }

        public Task Update()
        {
            return _updating = Task.Run(async () => {
                var regionsStream = await _apiService.GetNationStatesDumpStream(NationStatesDumpType.Regions);
                var nationsStream = await _apiService.GetNationStatesDumpStream(NationStatesDumpType.Nations);
                _regions = GetRegionsFromCompressedStream(regionsStream);
                _nations = GetNationsFromCompressedStream(nationsStream);
                AddNationsToRegions();
            });
        }
        
        private static List<REGION> GetRegionsFromCompressedStream(GZipStream stream)
        {
            Console.WriteLine("Extracting compressed stream to REGION Collection");
            return ParseRegionsFromCompressedStream(stream);
        }

        private static List<NATION> GetNationsFromCompressedStream(GZipStream stream)
        {
            Console.WriteLine("Extracting compressed stream to NATION Collection");
            return ParseNationsFromCompressedStream(stream);
        }

        private static List<REGION> ParseRegionsFromCompressedStream(GZipStream stream)
        {
            var xml = XDocument.Load(stream, LoadOptions.None);
            return xml.Descendants("REGION").Select(m =>

                new REGION
                {
                    Nodes = m.Elements(),
                    NAME = m.Element("NAME").Value,
                    NUMNATIONS = (int)m.Element("NUMNATIONS"),
                    NATIONNAMES = m.Element("NATIONS").Value.Split(":").ToList(),
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
                }).ToList();
        }

        private static List<OFFICER> BuildOfficers(XElement m)
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

        private static List<NATION> ParseNationsFromCompressedStream(GZipStream stream)
        {
            List<NATION> nations = new List<NATION>();
    
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

            return nations;
        }

        private static NATION BuildNation(XElement m)
        {
            return new NATION
            {
                Nodes = m.Elements(),
                NAME = m.Element("NAME").Value,
                TYPE = m.Element("TYPE").Value,
                FULLNAME = m.Element("FULLNAME").Value,
                MOTTO = m.Element("MOTTO").Value,
                CATEGORY = m.Element("CATEGORY").Value,
                WAMEMBER = m.Element("UNSTATUS").Value == "WA Member",
                ENDORSEMENTS = m.Element("ENDORSEMENTS").Value.Split(";").ToList(),
                FREEDOM = BuildFreedom(m),
                REGION = GetRegion(m.Element("REGION").Value),
                REGIONNAME = m.Element("REGION").Value,
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

        public static REGION GetRegion(string name)
        {
            return _regions.Find(r => r.NAME == name);
        }
        
        public static NATION GetNation(string name)
        {
            return _nations.Find(n => n.NAME == name);
        }
        
        private static void AddNationsToRegions()
        {
            foreach (var region in _regions)
            {
                region.NATIONNAMES.ForEach(name => region.NATIONS.Add(GetNation(name)));
            }
        }
    }
}
