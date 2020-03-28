using NationStatesAPIBot.Services;
using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace NationStatesAPIBot.DumpData
{
    public class NATION
    {
        public string NAME { get; set; }
        public string TYPE { get; set; }
        public string FULLNAME { get; set; }
        public string MOTTO { get; set; }
        public string CATEGORY { get; set; }
        bool isWA = false;
        public bool WAMEMBER
        {
            get
            {
                return isWA || (region != null && region.DELEGATE == BaseApiService.ToID(NAME));
            }
            set
            {
                isWA = value;
            }
        }
        public List<string> ENDORSEMENTS { get; set; }
        public FREEDOM FREEDOM { get; set; }
        private REGION region;
        public REGION REGION {
            get
            {
                return region;
            }
            set
            {
                region = value;
        
                if(region != null)
                {
                    if (region.NATIONS == null)
                    {
                        region.NATIONS = new HashSet<NATION>();
                    }
                    region.NATIONS.Add(this);
                }
            }
        }
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

        public override bool Equals(object obj)
        {
            return obj is NATION nation &&
                   NAME == nation.NAME &&
                   TYPE == nation.TYPE &&
                   FULLNAME == nation.FULLNAME &&
                   MOTTO == nation.MOTTO &&
                   CATEGORY == nation.CATEGORY &&
                   WAMEMBER == nation.WAMEMBER &&
                   EqualityComparer<List<string>>.Default.Equals(ENDORSEMENTS, nation.ENDORSEMENTS) &&
                   EqualityComparer<FREEDOM>.Default.Equals(FREEDOM, nation.FREEDOM) &&
                   EqualityComparer<REGION>.Default.Equals(REGION, nation.REGION) &&
                   REGIONNAME == nation.REGIONNAME &&
                   POPULATION == nation.POPULATION &&
                   TAX == nation.TAX &&
                   ANIMAL == nation.ANIMAL &&
                   CURRENCY == nation.CURRENCY &&
                   DEMONYM == nation.DEMONYM &&
                   DEMONYM2 == nation.DEMONYM2 &&
                   DEMONYM2PLURAL == nation.DEMONYM2PLURAL &&
                   FLAG == nation.FLAG &&
                   MAJORINDUSTRY == nation.MAJORINDUSTRY &&
                   GOVTPRIORITY == nation.GOVTPRIORITY &&
                   EqualityComparer<GOVT>.Default.Equals(GOVT, nation.GOVT) &&
                   FOUNDED == nation.FOUNDED &&
                   FIRSTLOGIN.Equals(nation.FIRSTLOGIN) &&
                   LASTLOGIN.Equals(nation.LASTLOGIN) &&
                   LASTACTIVITY == nation.LASTACTIVITY &&
                   INFLUENCE == nation.INFLUENCE &&
                   PUBLICSECTOR == nation.PUBLICSECTOR &&
                   EqualityComparer<DEATHS>.Default.Equals(DEATHS, nation.DEATHS) &&
                   LEADER == nation.LEADER &&
                   CAPITAL == nation.CAPITAL &&
                   RELIGION == nation.RELIGION &&
                   FACTBOOKS == nation.FACTBOOKS &&
                   DISPATCHES == nation.DISPATCHES &&
                   EqualityComparer<List<WABADGE>>.Default.Equals(WABADGES, nation.WABADGES) &&
                   CARDCATEGORY == nation.CARDCATEGORY;
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(NAME);
            hash.Add(TYPE);
            hash.Add(FULLNAME);
            hash.Add(MOTTO);
            hash.Add(CATEGORY);
            hash.Add(WAMEMBER);
            hash.Add(ENDORSEMENTS);
            hash.Add(FREEDOM);
            hash.Add(REGION);
            hash.Add(REGIONNAME);
            hash.Add(POPULATION);
            hash.Add(TAX);
            hash.Add(ANIMAL);
            hash.Add(CURRENCY);
            hash.Add(DEMONYM);
            hash.Add(DEMONYM2);
            hash.Add(DEMONYM2PLURAL);
            hash.Add(FLAG);
            hash.Add(MAJORINDUSTRY);
            hash.Add(GOVTPRIORITY);
            hash.Add(GOVT);
            hash.Add(FOUNDED);
            hash.Add(FIRSTLOGIN);
            hash.Add(LASTLOGIN);
            hash.Add(LASTACTIVITY);
            hash.Add(INFLUENCE);
            hash.Add(PUBLICSECTOR);
            hash.Add(DEATHS);
            hash.Add(LEADER);
            hash.Add(CAPITAL);
            hash.Add(RELIGION);
            hash.Add(FACTBOOKS);
            hash.Add(DISPATCHES);
            hash.Add(WABADGES);
            hash.Add(CARDCATEGORY);
            return hash.ToHashCode();
        }
    }


}
