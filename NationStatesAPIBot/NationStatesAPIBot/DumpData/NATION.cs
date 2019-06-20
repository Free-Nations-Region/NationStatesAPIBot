using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace DumpData
{
    public class NATION
    {
        public IEnumerable<XElement> Nodes { get; set; }
        public string NAME { get; set; }
        public string TYPE { get; set; }
        public string FULLNAME { get; set; }
        public string MOTTO { get; set; }
        public string CATEGORY { get; set; }
        public bool WAMEMBER { get; set; }
        public List<string> ENDORSEMENTS { get; set; }
        public FREEDOM FREEDOM { get; set; }
        public REGION REGION { get; set; }
        public string REGIONNAME { get; set; }
        public double POPULATION { get; set; }
        public double TAX { get; set; }
        public string ANIMAL { get; set; }
        public string CURRENCY { get; set; }
        public string DEMONYM { get; set; }
        public string DEMONYM2 { get; set; }
        public string DEMONYM2PLURAL { get; set; }
        public string FLAG { get; set; }
        public string MAJORINDUSTRY { get; set; }
        public string GOVTPRIORITY { get; set; }
        public GOVT GOVT { get; set; }
        public string FOUNDED { get; set; }
        public DateTimeOffset FIRSTLOGIN { get; set; }
        public DateTimeOffset LASTLOGIN { get; set; }
        public string LASTACTIVITY { get; set; }
        public string INFLUENCE { get; set; }
        public double PUBLICSECTOR { get; set; }
        public DEATHS DEATHS { get; set; }
        public string LEADER { get; set; }
        public string CAPITAL { get; set; }
        public string RELIGION { get; set; }
        public int FACTBOOKS { get; set; }
        public int DISPATCHES { get; set; }
        public List<WABADGE> WABADGES { get; set; }
        public string CARDCATEGORY { get; set; }
    }


}
