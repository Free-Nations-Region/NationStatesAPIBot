using System;

namespace CyborgianStates.DumpData
{
    public class GOVT
    {
        public double ADMINISTRATION { get; set; }
        public double DEFENCE { get; set; }
        public double EDUCATION { get; set; }
        public double ENVIRONMENT { get; set; }
        public double HEALTHCARE { get; set; }
        public double COMMERCE { get; set; }
        public double INTERNATIONALAID { get; set; }
        public double LAWANDORDER { get; set; }
        public double PUBLICTRANSPORT { get; set; }
        public double SOCIALEQUALITY { get; set; }
        public double SPIRITUALITY { get; set; }
        public double WELFARE { get; set; }

        public override bool Equals(object obj)
        {
            return obj is GOVT govt &&
                   ADMINISTRATION == govt.ADMINISTRATION &&
                   DEFENCE == govt.DEFENCE &&
                   EDUCATION == govt.EDUCATION &&
                   ENVIRONMENT == govt.ENVIRONMENT &&
                   HEALTHCARE == govt.HEALTHCARE &&
                   COMMERCE == govt.COMMERCE &&
                   INTERNATIONALAID == govt.INTERNATIONALAID &&
                   LAWANDORDER == govt.LAWANDORDER &&
                   PUBLICTRANSPORT == govt.PUBLICTRANSPORT &&
                   SOCIALEQUALITY == govt.SOCIALEQUALITY &&
                   SPIRITUALITY == govt.SPIRITUALITY &&
                   WELFARE == govt.WELFARE;
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(ADMINISTRATION);
            hash.Add(DEFENCE);
            hash.Add(EDUCATION);
            hash.Add(ENVIRONMENT);
            hash.Add(HEALTHCARE);
            hash.Add(COMMERCE);
            hash.Add(INTERNATIONALAID);
            hash.Add(LAWANDORDER);
            hash.Add(PUBLICTRANSPORT);
            hash.Add(SOCIALEQUALITY);
            hash.Add(SPIRITUALITY);
            hash.Add(WELFARE);
            return hash.ToHashCode();
        }
    }
}
