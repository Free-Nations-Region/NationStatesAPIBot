using System;

namespace CyborgianStates.DumpData
{
    public class OFFICER
    {
        public string NATION { get; set; }
        public string OFFICE { get; set; }
        public string AUTHORITY { get; set; }
        public DateTimeOffset TIME { get; set; }
        public string BY { get; set; }
        public int ORDER { get; set; }

        public override bool Equals(object obj)
        {
            return obj is OFFICER oFFICER &&
                   NATION == oFFICER.NATION &&
                   OFFICE == oFFICER.OFFICE &&
                   AUTHORITY == oFFICER.AUTHORITY &&
                   TIME.Equals(oFFICER.TIME) &&
                   BY == oFFICER.BY &&
                   ORDER == oFFICER.ORDER;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(NATION, OFFICE, AUTHORITY, TIME, BY, ORDER);
        }
    }
}
