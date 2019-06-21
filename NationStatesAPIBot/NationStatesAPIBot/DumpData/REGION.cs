using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace NationStatesAPIBot.DumpData
{
    public class REGION
    {
        public IEnumerable<XElement> Nodes { get; set; }
        public string NAME { get; set; }
        public int NUMNATIONS { get; set; }
        public List<string> NATIONNAMES { get; set; }
        public List<NATION> NATIONS { get; set; }
        public string DELEGATE { get; set; }
        public int DELEGATEVOTES { get; set; }
        public string DELEGATEAUTH { get; set; }
        public string FOUNDER { get; set; }
        public string FOUNDERAUTH { get; set; }
        public List<OFFICER> OFFICERS { get; set; }
        public string POWER { get; set; }
        public string FLAG { get; set; }
        public List<string> EMBASSIES { get; set; }
        public DateTimeOffset LASTUPDATE { get; set; }
        public List<WABADGE> WABADGES { get; set; }
    }
}
