using System;
using System.Collections.Generic;

namespace NationStatesAPIBot.DumpData
{
    public class DEATHS
    {
        public List<CAUSE> CAUSE { get; set; }

        public override bool Equals(object obj)
        {
            return obj is DEATHS deaths &&
                   EqualityComparer<List<CAUSE>>.Default.Equals(CAUSE, deaths.CAUSE);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(CAUSE);
        }
    }


}
