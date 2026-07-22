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
            if (HasActiveSignal(signal.Setup))
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

        public bool HasActiveSignal(SignalSetup setup)
        {
            foreach (TrackedSignal trackedSignal in signals)
            {
                if (trackedSignal.IsActive && trackedSignal.Signal.Setup == setup)
                    return true;
            }

            return false;
        }

        public TrackedSignal RejectByRisk(TradeSignal signal, int currentBar)
        {
            TrackedSignal trackedSignal = new TrackedSignal(signal, currentBar);
            trackedSignal.Status = SignalStatus.RiskRejected;
            trackedSignal.ClosedAt = signal.CreatedAt;
            trackedSignal.TargetOneRStatus = ComparisonStatus.RiskRejected;
            trackedSignal.TargetOneRAt = signal.CreatedAt;
            trackedSignal.TargetOnePointFiveRStatus = ComparisonStatus.RiskRejected;
            trackedSignal.TargetOnePointFiveRAt = signal.CreatedAt;
            trackedSignal.TargetTwoRStatus = ComparisonStatus.RiskRejected;
            trackedSignal.TargetTwoRAt = signal.CreatedAt;
            trackedSignal.FirstEvent = FirstOutcomeEvent.RiskRejected;
            trackedSignal.FirstEventAt = signal.CreatedAt;
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
                bool expired = trackedSignal.BarsElapsed >= trackedSignal.Signal.ValidForBars;

                UpdateComparativeOutcomes(trackedSignal, high, low, stopHit, expired, time);

                if (targetHit && stopHit)
                    Close(trackedSignal, SignalStatus.Ambiguous, 0, time, closedSignals);
                else if (targetHit)
                    Close(trackedSignal, SignalStatus.TargetHit, trackedSignal.Signal.RiskRewardRatio, time, closedSignals);
                else if (stopHit)
                    Close(trackedSignal, SignalStatus.StopHit, -1, time, closedSignals);
                else if (expired)
                    Close(trackedSignal, SignalStatus.Expired, 0, time, closedSignals);
            }

            return closedSignals;
        }

        public TrackedSignal GetLastSignal()
        {
            return signals.Count == 0 ? null : signals[signals.Count - 1];
        }

        public TrackedSignal GetLastSignal(SignalSetup setup)
        {
            for (int index = signals.Count - 1; index >= 0; index--)
            {
                if (signals[index].Signal.Setup == setup)
                    return signals[index];
            }

            return null;
        }

        public SignalStatistics GetStatistics()
        {
            return GetStatistics(null);
        }

        public SignalStatistics GetStatistics(SignalSetup setup)
        {
            return GetStatistics((SignalSetup?)setup);
        }

        private SignalStatistics GetStatistics(SignalSetup? setup)
        {
            SignalStatistics statistics = new SignalStatistics();

            foreach (TrackedSignal trackedSignal in signals)
            {
                if (setup.HasValue && trackedSignal.Signal.Setup != setup.Value)
                    continue;

                statistics.Total++;
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

        private static void UpdateComparativeOutcomes(
            TrackedSignal trackedSignal,
            double high,
            double low,
            bool stopHit,
            bool expired,
            DateTime time)
        {
            bool targetOneRHit = IsPriceTargetHit(trackedSignal.Signal, trackedSignal.Signal.TargetOneRPrice, high, low);
            bool targetOnePointFiveRHit = IsPriceTargetHit(trackedSignal.Signal, trackedSignal.Signal.TargetOnePointFiveRPrice, high, low);
            bool targetTwoRHit = IsPriceTargetHit(trackedSignal.Signal, trackedSignal.Signal.TargetTwoRPrice, high, low);

            if (trackedSignal.FirstEvent == FirstOutcomeEvent.Pending)
            {
                if (targetOneRHit && stopHit)
                    SetFirstEvent(trackedSignal, FirstOutcomeEvent.Ambiguous, time);
                else if (targetOneRHit)
                    SetFirstEvent(trackedSignal, FirstOutcomeEvent.Target1R, time);
                else if (stopHit)
                    SetFirstEvent(trackedSignal, FirstOutcomeEvent.Stop, time);
                else if (expired)
                    SetFirstEvent(trackedSignal, FirstOutcomeEvent.Expired, time);
            }

            UpdateComparison(
                trackedSignal.TargetOneRStatus,
                targetOneRHit,
                stopHit,
                expired,
                time,
                delegate(ComparisonStatus status, DateTime at)
                {
                    trackedSignal.TargetOneRStatus = status;
                    trackedSignal.TargetOneRAt = at;
                });
            UpdateComparison(
                trackedSignal.TargetOnePointFiveRStatus,
                targetOnePointFiveRHit,
                stopHit,
                expired,
                time,
                delegate(ComparisonStatus status, DateTime at)
                {
                    trackedSignal.TargetOnePointFiveRStatus = status;
                    trackedSignal.TargetOnePointFiveRAt = at;
                });
            UpdateComparison(
                trackedSignal.TargetTwoRStatus,
                targetTwoRHit,
                stopHit,
                expired,
                time,
                delegate(ComparisonStatus status, DateTime at)
                {
                    trackedSignal.TargetTwoRStatus = status;
                    trackedSignal.TargetTwoRAt = at;
                });
        }

        private static void UpdateComparison(
            ComparisonStatus currentStatus,
            bool targetHit,
            bool stopHit,
            bool expired,
            DateTime time,
            Action<ComparisonStatus, DateTime> update)
        {
            if (currentStatus != ComparisonStatus.Pending)
                return;

            if (targetHit && stopHit)
                update(ComparisonStatus.Ambiguous, time);
            else if (targetHit)
                update(ComparisonStatus.TargetHit, time);
            else if (stopHit)
                update(ComparisonStatus.StopHit, time);
            else if (expired)
                update(ComparisonStatus.Expired, time);
        }

        private static bool IsPriceTargetHit(TradeSignal signal, double targetPrice, double high, double low)
        {
            return signal.Direction == SignalDirection.Long
                ? high >= targetPrice
                : low <= targetPrice;
        }

        private static void SetFirstEvent(TrackedSignal trackedSignal, FirstOutcomeEvent firstEvent, DateTime time)
        {
            trackedSignal.FirstEvent = firstEvent;
            trackedSignal.FirstEventAt = time;
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
