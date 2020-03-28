using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace NationStatesAPIBot.DumpData
{
    public class REGION
    {
        public string NAME { get; set; }
        public int DumpPosition { get; set; }
        public int NUMNATIONS { get; set; }
        public HashSet<string> NATIONNAMES { get; set; }
        public HashSet<NATION> NATIONS { get; set; }
        public IEnumerable<NATION> WANATIONS
        {
            get
            {
                return NATIONS.Where(n => n.WAMEMBER);
            }
        }
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

        public override bool Equals(object obj)
        {
            return obj is REGION region &&
                   NAME == region.NAME &&
                   DumpPosition == region.DumpPosition &&
                   NUMNATIONS == region.NUMNATIONS &&
                   EqualityComparer<HashSet<string>>.Default.Equals(NATIONNAMES, region.NATIONNAMES) &&
                   EqualityComparer<HashSet<NATION>>.Default.Equals(NATIONS, region.NATIONS) &&
                   DELEGATE == region.DELEGATE &&
                   DELEGATEVOTES == region.DELEGATEVOTES &&
                   DELEGATEAUTH == region.DELEGATEAUTH &&
                   FOUNDER == region.FOUNDER &&
                   FOUNDERAUTH == region.FOUNDERAUTH &&
                   EqualityComparer<List<OFFICER>>.Default.Equals(OFFICERS, region.OFFICERS) &&
                   POWER == region.POWER &&
                   FLAG == region.FLAG &&
                   EqualityComparer<List<string>>.Default.Equals(EMBASSIES, region.EMBASSIES) &&
                   LASTUPDATE.Equals(region.LASTUPDATE) &&
                   EqualityComparer<List<WABADGE>>.Default.Equals(WABADGES, region.WABADGES);
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(NAME);
            hash.Add(DumpPosition);
            hash.Add(NUMNATIONS);
            hash.Add(NATIONNAMES);
            hash.Add(NATIONS);
            hash.Add(DELEGATE);
            hash.Add(DELEGATEVOTES);
            hash.Add(DELEGATEAUTH);
            hash.Add(FOUNDER);
            hash.Add(FOUNDERAUTH);
            hash.Add(OFFICERS);
            hash.Add(POWER);
            hash.Add(FLAG);
            hash.Add(EMBASSIES);
            hash.Add(LASTUPDATE);
            hash.Add(WABADGES);
            return hash.ToHashCode();
        }
    }
}
