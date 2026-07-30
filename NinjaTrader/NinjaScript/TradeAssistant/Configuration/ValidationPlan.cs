using System;
using NinjaTrader.NinjaScript.TradeAssistant.Models;

namespace NinjaTrader.NinjaScript.TradeAssistant.Configuration
{
    public static class ValidationPlan
    {
        public const string RoundId = "qualified-149d-2026-07-v5";
        public const int EmaSlopeLookbackBars = 3;
        public const int VolumeAveragePeriod = 20;
        public const int MinimumSessions = 10;
        public const int MinimumDecidedSignals = 20;
        public const int QualifiedMinimumScore = 5;
        public const int QualifiedValidForBars = 12;
        public const double QualifiedMaximumVwapDistanceAtr = 2.0;
        public const double QualifiedMinimumRelativeVolume = 1.0;
        public const double QualifiedTargetR = 1.5;
        public const double FrozenMinimumRiskPerContract = 5.0;
        public const double FrozenMaximumRiskPerContract = 50.0;
        public static readonly DateTime ForwardStartDate = new DateTime(2026, 7, 30);

        public static bool IsForwardSample(DateTime signalTime)
        {
            return signalTime.Date >= ForwardStartDate.Date;
        }

        public static string GetSamplePhase(DateTime signalTime)
        {
            return IsForwardSample(signalTime) ? "Forward" : "HistoricalReference";
        }

        public static ValidationProfile GetProfile(
            string instrument,
            SignalSetup setup,
            double configuredTargetR)
        {
            string masterInstrument = GetMasterInstrument(instrument);

            if (masterInstrument == "MES")
            {
                double mesTarget = setup == SignalSetup.QualifiedPullback
                    ? QualifiedTargetR
                    : 1.0;
                return new ValidationProfile(setup, ValidationStage.Reference, mesTarget, false);
            }

            if (masterInstrument == "MNQ")
            {
                if (setup == SignalSetup.QualifiedPullback)
                    return new ValidationProfile(
                        setup,
                        ValidationStage.Candidate,
                        QualifiedTargetR,
                        true);

                return new ValidationProfile(setup, ValidationStage.Reference, 1.0, false);
            }

            return new ValidationProfile(
                setup,
                ValidationStage.Reference,
                setup == SignalSetup.QualifiedPullback ? QualifiedTargetR : 1.0,
                false);
        }

        public static ValidationProfile GetPrimaryProfile(string instrument, double configuredTargetR)
        {
            return GetProfile(instrument, SignalSetup.QualifiedPullback, configuredTargetR);
        }

        public static bool MatchesEvidenceRule(
            string instrument,
            SignalDirection direction,
            double entryPrice,
            SignalContext context)
        {
            return GetMasterInstrument(instrument) == "MNQ"
                && direction == SignalDirection.Short
                && context != null
                && context.Score >= 4
                && entryPrice < context.SessionVwap
                && context.VwapSlopeAtr < 0;
        }

        public static bool MatchesQualifiedRule(
            string instrument,
            SignalDirection direction,
            SignalContext context)
        {
            return GetMasterInstrument(instrument) == "MNQ"
                && direction == SignalDirection.Short
                && context != null
                && context.Score >= QualifiedMinimumScore
                && context.VwapDistanceAtr <= QualifiedMaximumVwapDistanceAtr
                && context.RelativeVolume >= QualifiedMinimumRelativeVolume;
        }

        public static bool IsQualifiedRiskEligible(double riskCurrency)
        {
            return riskCurrency >= FrozenMinimumRiskPerContract
                && riskCurrency <= FrozenMaximumRiskPerContract;
        }

        public static double NormalizeMaximumRiskPerContract(
            double configuredMaximumRiskPerContract,
            RiskLimitMode riskLimitMode)
        {
            if (riskLimitMode == RiskLimitMode.DescartarAcimaDoLimite
                && configuredMaximumRiskPerContract > FrozenMaximumRiskPerContract)
            {
                return FrozenMaximumRiskPerContract;
            }

            return configuredMaximumRiskPerContract;
        }

        public static bool MatchesFrozenConfiguration(
            int fastEmaPeriod,
            int slowEmaPeriod,
            int atrPeriod,
            double pullbackToleranceAtr,
            int pullbackCooldownBars,
            double stopAtrMultiplier,
            double configuredTargetR,
            int validForBars,
            double maximumRiskPerContract,
            RiskLimitMode riskLimitMode)
        {
            return fastEmaPeriod == 9
                && slowEmaPeriod == 21
                && atrPeriod == 14
                && NearlyEqual(pullbackToleranceAtr, 0.1)
                && pullbackCooldownBars == 3
                && NearlyEqual(stopAtrMultiplier, 1.5)
                && NearlyEqual(configuredTargetR, 2.0)
                && validForBars == 3
                && NearlyEqual(maximumRiskPerContract, FrozenMaximumRiskPerContract)
                && riskLimitMode == RiskLimitMode.DescartarAcimaDoLimite;
        }

        private static string GetMasterInstrument(string instrument)
        {
            if (string.IsNullOrWhiteSpace(instrument))
                return string.Empty;

            string trimmed = instrument.Trim();
            int separator = trimmed.IndexOf(' ');
            return (separator < 0 ? trimmed : trimmed.Substring(0, separator)).ToUpperInvariant();
        }

        private static bool NearlyEqual(double left, double right)
        {
            return Math.Abs(left - right) < 0.0000001;
        }
    }
}
