using System;

namespace CyborgianStates.DumpData
{
    public class CAUSE
    {
        public string Type { get; set; }
        public double Value { get; set; }

        public override bool Equals(object obj)
        {
            return obj is CAUSE cause &&
                   Type == cause.Type &&
                   Value == cause.Value;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Type, Value);
        }
    }


}
