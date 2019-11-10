using System;
using System.Collections.Generic;

namespace CyborgianStates.DumpData
{
    public class DEATHS
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Sammlungseigenschaften müssen schreibgeschützt sein", Justification = "Needs to be set by DumpDataService")]
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
