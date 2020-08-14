using System;
using System.Text;

namespace NationStatesAPIBot.Commands.Management
{
    public class RNStatus
    {
        internal string IssuedBy { get; set; }
        internal DateTimeOffset StartedAt { get; set; }
        internal TimeSpan AvgTimePerFoundNation { get; set; }
        internal int FinalCount { get; set; }
        internal int SkippedCount { get; set; }
        private int current = 0;

        internal int CurrentCount
        {
            get
            {
                return current;
            }
            set
            {
                current = value;
                var now = DateTimeOffset.UtcNow;
                var timespend = now.Subtract(StartedAt);
                AvgTimePerFoundNation = timespend.Divide(CurrentCount);
            }
        }

        internal TimeSpan ExpectedIn()
        {
            var remaining = FinalCount - CurrentCount;
            return AvgTimePerFoundNation.Multiply(remaining);
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"--- Status of current /rn command ---");
            builder.AppendLine($"Issued by: {IssuedBy}");
            builder.AppendLine($"Time spend (mm:ss): {DateTimeOffset.UtcNow.Subtract(StartedAt):mm\\:ss}");
            builder.AppendLine($"Current status: {CurrentCount}/{FinalCount}");
            builder.AppendLine($"Nations skipped: {SkippedCount}");
            builder.AppendLine($"Avg. Time per found nation (mm:ss): {AvgTimePerFoundNation:mm\\:ss}");
            builder.AppendLine($"Finish expected in approx. (mm:ss): {ExpectedIn():mm\\:ss}");
            builder.AppendLine($"--- End of status ---");
            return builder.ToString();
        }
    }
}