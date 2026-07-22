using System;

namespace NinjaTrader.NinjaScript.TradeAssistant.Models
{
    public sealed class TrackedSignal
    {
        public TrackedSignal(TradeSignal signal, int createdBar)
        {
            Signal = signal;
            CreatedBar = createdBar;
            Status = SignalStatus.Active;
            TargetOneRStatus = ComparisonStatus.Pending;
            TargetOnePointFiveRStatus = ComparisonStatus.Pending;
            TargetTwoRStatus = ComparisonStatus.Pending;
            FirstEvent = FirstOutcomeEvent.Pending;
        }

        public TradeSignal Signal { get; private set; }
        public int CreatedBar { get; private set; }
        public int BarsElapsed { get; internal set; }
        public SignalStatus Status { get; internal set; }
        public DateTime? ClosedAt { get; internal set; }
        public double ResultR { get; internal set; }
        public double MaximumFavorableExcursionR { get; internal set; }
        public double MaximumAdverseExcursionR { get; internal set; }
        public ComparisonStatus TargetOneRStatus { get; internal set; }
        public DateTime? TargetOneRAt { get; internal set; }
        public ComparisonStatus TargetOnePointFiveRStatus { get; internal set; }
        public DateTime? TargetOnePointFiveRAt { get; internal set; }
        public ComparisonStatus TargetTwoRStatus { get; internal set; }
        public DateTime? TargetTwoRAt { get; internal set; }
        public FirstOutcomeEvent FirstEvent { get; internal set; }
        public DateTime? FirstEventAt { get; internal set; }

        public bool IsActive
        {
            get { return Status == SignalStatus.Active; }
        }
    }
}
