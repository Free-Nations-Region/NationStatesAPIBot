using System;

namespace NationStatesAPIBot.DumpData
{
    public class FREEDOM
    {
        public string CIVILRIGHTS { get; set; }
        public double CIVILRIGHTS_SCORE { get; set; }
        public string ECONOMY { get; set; }
        public double ECONOMY_SCORE { get; set; }
        public string POLITICALFREEDOM { get; set; }
        public double POLITICALFREEDOM_SCORE { get; set; }

        public override bool Equals(object obj)
        {
            return obj is FREEDOM freedom &&
                   CIVILRIGHTS == freedom.CIVILRIGHTS &&
                   CIVILRIGHTS_SCORE == freedom.CIVILRIGHTS_SCORE &&
                   ECONOMY == freedom.ECONOMY &&
                   ECONOMY_SCORE == freedom.ECONOMY_SCORE &&
                   POLITICALFREEDOM == freedom.POLITICALFREEDOM &&
                   POLITICALFREEDOM_SCORE == freedom.POLITICALFREEDOM_SCORE;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(CIVILRIGHTS, CIVILRIGHTS_SCORE, ECONOMY, ECONOMY_SCORE, POLITICALFREEDOM, POLITICALFREEDOM_SCORE);
        }
    }


}
