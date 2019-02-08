using System;

namespace NationStatesAPIBot.Entities
{
    public class Nation
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public int StatusId { get; set; }
        public NationStatus Status { get; set; }
        public DateTime StatusTime { get; set; }
    }
}
