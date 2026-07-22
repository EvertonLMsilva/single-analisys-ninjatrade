using System;
using System.Collections.Generic;
using NinjaTrader.NinjaScript.TradeAssistant.Models;

namespace NinjaTrader.NinjaScript.TradeAssistant.Analysis
{
    public static class ValidationStatisticsCalculator
    {
        public static ValidationStatistics Calculate(
            IEnumerable<TrackedSignal> signals,
            SignalSetup setup,
            double targetR,
            DateTime day)
        {
            ValidationStatistics statistics = new ValidationStatistics();
            List<TrackedSignal> ordered = new List<TrackedSignal>();

            foreach (TrackedSignal trackedSignal in signals)
            {
                if (trackedSignal.Signal.Setup != setup
                    || trackedSignal.Signal.CreatedAt.Date != day.Date)
                    continue;

                statistics.Total++;
                ordered.Add(trackedSignal);
            }

            ordered.Sort(delegate(TrackedSignal left, TrackedSignal right)
            {
                return left.Signal.CreatedAt.CompareTo(right.Signal.CreatedAt);
            });

            double winnerRiskTotal = 0;
            double loserRiskTotal = 0;
            double equityR = 0;
            double peakR = 0;
            double equityCurrency = 0;
            double peakCurrency = 0;
            int consecutiveLosses = 0;

            foreach (TrackedSignal trackedSignal in ordered)
            {
                ComparisonStatus status = GetStatus(trackedSignal, targetR);

                switch (status)
                {
                    case ComparisonStatus.Pending:
                        statistics.Active++;
                        break;
                    case ComparisonStatus.TargetHit:
                        statistics.TargetHits++;
                        statistics.ResultR += targetR;
                        statistics.ResultCurrency += trackedSignal.Signal.RiskCurrency * targetR;
                        winnerRiskTotal += trackedSignal.Signal.RiskCurrency;
                        consecutiveLosses = 0;
                        equityR += targetR;
                        equityCurrency += trackedSignal.Signal.RiskCurrency * targetR;
                        break;
                    case ComparisonStatus.StopHit:
                        statistics.StopHits++;
                        statistics.ResultR -= 1.0;
                        statistics.ResultCurrency -= trackedSignal.Signal.RiskCurrency;
                        loserRiskTotal += trackedSignal.Signal.RiskCurrency;
                        consecutiveLosses++;
                        statistics.MaximumConsecutiveLosses = Math.Max(
                            statistics.MaximumConsecutiveLosses,
                            consecutiveLosses);
                        equityR -= 1.0;
                        equityCurrency -= trackedSignal.Signal.RiskCurrency;
                        break;
                    case ComparisonStatus.Expired:
                        statistics.Expired++;
                        consecutiveLosses = 0;
                        break;
                    case ComparisonStatus.Ambiguous:
                        statistics.Ambiguous++;
                        consecutiveLosses = 0;
                        break;
                    case ComparisonStatus.RiskRejected:
                        statistics.RiskRejected++;
                        break;
                }

                peakR = Math.Max(peakR, equityR);
                peakCurrency = Math.Max(peakCurrency, equityCurrency);
                statistics.MaximumDrawdownR = Math.Max(
                    statistics.MaximumDrawdownR,
                    peakR - equityR);
                statistics.MaximumDrawdownCurrency = Math.Max(
                    statistics.MaximumDrawdownCurrency,
                    peakCurrency - equityCurrency);
            }

            if (statistics.TargetHits > 0)
                statistics.AverageWinnerRiskCurrency = winnerRiskTotal / statistics.TargetHits;
            if (statistics.StopHits > 0)
                statistics.AverageLoserRiskCurrency = loserRiskTotal / statistics.StopHits;

            return statistics;
        }

        public static ComparisonStatus GetStatus(TrackedSignal trackedSignal, double targetR)
        {
            if (Math.Abs(targetR - 1.0) < 0.0000001)
                return trackedSignal.TargetOneRStatus;
            if (Math.Abs(targetR - 1.5) < 0.0000001)
                return trackedSignal.TargetOnePointFiveRStatus;
            if (Math.Abs(targetR - 2.0) < 0.0000001)
                return trackedSignal.TargetTwoRStatus;

            return ComparisonStatus.Pending;
        }
    }
}
