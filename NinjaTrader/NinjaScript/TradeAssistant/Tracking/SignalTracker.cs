using System;
using System.Collections.Generic;
using NinjaTrader.NinjaScript.TradeAssistant.Models;

namespace NinjaTrader.NinjaScript.TradeAssistant.Tracking
{
    public sealed class SignalTracker
    {
        private readonly List<TrackedSignal> signals = new List<TrackedSignal>();

        public TrackedSignal Register(TradeSignal signal, int currentBar)
        {
            if (HasActiveSignal())
                return null;

            TrackedSignal trackedSignal = new TrackedSignal(signal, currentBar);
            signals.Add(trackedSignal);
            return trackedSignal;
        }

        public bool HasActiveSignal()
        {
            foreach (TrackedSignal trackedSignal in signals)
            {
                if (trackedSignal.IsActive)
                    return true;
            }

            return false;
        }

        public TrackedSignal RejectByRisk(TradeSignal signal, int currentBar)
        {
            TrackedSignal trackedSignal = new TrackedSignal(signal, currentBar);
            trackedSignal.Status = SignalStatus.RiskRejected;
            trackedSignal.ClosedAt = signal.CreatedAt;
            signals.Add(trackedSignal);
            return trackedSignal;
        }

        public IList<TrackedSignal> Update(double high, double low, DateTime time, int currentBar)
        {
            List<TrackedSignal> closedSignals = new List<TrackedSignal>();

            foreach (TrackedSignal trackedSignal in signals)
            {
                if (!trackedSignal.IsActive || currentBar <= trackedSignal.CreatedBar)
                    continue;

                trackedSignal.BarsElapsed = currentBar - trackedSignal.CreatedBar;
                UpdateExcursions(trackedSignal, high, low);

                bool targetHit = IsTargetHit(trackedSignal.Signal, high, low);
                bool stopHit = IsStopHit(trackedSignal.Signal, high, low);

                if (targetHit && stopHit)
                    Close(trackedSignal, SignalStatus.Ambiguous, 0, time, closedSignals);
                else if (targetHit)
                    Close(trackedSignal, SignalStatus.TargetHit, trackedSignal.Signal.RiskRewardRatio, time, closedSignals);
                else if (stopHit)
                    Close(trackedSignal, SignalStatus.StopHit, -1, time, closedSignals);
                else if (trackedSignal.BarsElapsed >= trackedSignal.Signal.ValidForBars)
                    Close(trackedSignal, SignalStatus.Expired, 0, time, closedSignals);
            }

            return closedSignals;
        }

        public TrackedSignal GetLastSignal()
        {
            return signals.Count == 0 ? null : signals[signals.Count - 1];
        }

        public SignalStatistics GetStatistics()
        {
            SignalStatistics statistics = new SignalStatistics();
            statistics.Total = signals.Count;

            foreach (TrackedSignal trackedSignal in signals)
            {
                switch (trackedSignal.Status)
                {
                    case SignalStatus.Active:
                        statistics.Active++;
                        break;
                    case SignalStatus.TargetHit:
                        statistics.TargetHits++;
                        statistics.TotalR += trackedSignal.ResultR;
                        break;
                    case SignalStatus.StopHit:
                        statistics.StopHits++;
                        statistics.TotalR += trackedSignal.ResultR;
                        break;
                    case SignalStatus.Expired:
                        statistics.Expired++;
                        break;
                    case SignalStatus.Ambiguous:
                        statistics.Ambiguous++;
                        break;
                    case SignalStatus.RiskRejected:
                        statistics.RiskRejected++;
                        break;
                }
            }

            return statistics;
        }

        private static void UpdateExcursions(TrackedSignal trackedSignal, double high, double low)
        {
            double risk = trackedSignal.Signal.Risk;
            if (risk <= 0)
                return;

            double favorable;
            double adverse;

            if (trackedSignal.Signal.Direction == SignalDirection.Long)
            {
                favorable = Math.Max(0, high - trackedSignal.Signal.EntryPrice) / risk;
                adverse = Math.Max(0, trackedSignal.Signal.EntryPrice - low) / risk;
            }
            else
            {
                favorable = Math.Max(0, trackedSignal.Signal.EntryPrice - low) / risk;
                adverse = Math.Max(0, high - trackedSignal.Signal.EntryPrice) / risk;
            }

            trackedSignal.MaximumFavorableExcursionR = Math.Max(trackedSignal.MaximumFavorableExcursionR, favorable);
            trackedSignal.MaximumAdverseExcursionR = Math.Max(trackedSignal.MaximumAdverseExcursionR, adverse);
        }

        private static bool IsTargetHit(TradeSignal signal, double high, double low)
        {
            return signal.Direction == SignalDirection.Long
                ? high >= signal.TargetPrice
                : low <= signal.TargetPrice;
        }

        private static bool IsStopHit(TradeSignal signal, double high, double low)
        {
            return signal.Direction == SignalDirection.Long
                ? low <= signal.StopPrice
                : high >= signal.StopPrice;
        }

        private static void Close(
            TrackedSignal trackedSignal,
            SignalStatus status,
            double resultR,
            DateTime time,
            ICollection<TrackedSignal> closedSignals)
        {
            trackedSignal.Status = status;
            trackedSignal.ResultR = resultR;
            trackedSignal.ClosedAt = time;
            closedSignals.Add(trackedSignal);
        }
    }
}
