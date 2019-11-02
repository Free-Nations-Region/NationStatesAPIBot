using System;

namespace CyborgianStates.DumpData
{
    public class WABADGE
    {
        public string Type { get; set; }
        public int Value { get; set; }

        public override bool Equals(object obj)
        {
            return obj is WABADGE wabadge &&
                   Type == wabadge.Type &&
                   Value == wabadge.Value;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Type, Value);
        }
    }
}
