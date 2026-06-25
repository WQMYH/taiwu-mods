using System;

namespace AutoMonthlyEvent.Executor.Frontend
{
    internal sealed class EventDecision
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string EventGuid { get; set; } = string.Empty;
        public string CandidateType { get; set; } = string.Empty;
        public int RequesterCharacterId { get; set; } = -1;
        public string Decision { get; set; } = "skip";
        public string OptionKey { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public ushort RelationType { get; set; }
        public short Favorability { get; set; } = short.MinValue;
        public bool RelationResolved { get; set; }
        public bool DryRun { get; set; }
        public bool Skipped { get; set; } = true;

        public string ToJsonLine()
        {
            return "{"
                + $"\"timestamp\":\"{Timestamp:O}\","
                + $"\"eventGuid\":\"{ActionLogger.Escape(EventGuid)}\","
                + $"\"candidateType\":\"{ActionLogger.Escape(CandidateType)}\","
                + $"\"requesterCharacterId\":{RequesterCharacterId},"
                + $"\"decision\":\"{ActionLogger.Escape(Decision)}\","
                + $"\"optionKey\":\"{ActionLogger.Escape(OptionKey)}\","
                + $"\"reason\":\"{ActionLogger.Escape(Reason)}\","
                + $"\"relationType\":{RelationType},"
                + $"\"favorability\":{Favorability},"
                + $"\"relationResolved\":{RelationResolved.ToString().ToLowerInvariant()},"
                + $"\"dryRun\":{DryRun.ToString().ToLowerInvariant()},"
                + $"\"skipped\":{Skipped.ToString().ToLowerInvariant()}"
                + "}";
        }
    }
}
