using System;
using NinjaTrader.NinjaScript.TradeAssistant.Models;

namespace NinjaTrader.NinjaScript.TradeAssistant.Configuration
{
    public static class ValidationPlan
    {
        public const string RoundId = "evidence-2026-07-v4";
        public const int EmaSlopeLookbackBars = 3;
        public const int VolumeAveragePeriod = 20;
        public const int MinimumSessions = 5;
        public const int MinimumDecidedSignals = 30;
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
                return new ValidationProfile(setup, ValidationStage.Reference, 1.0, false);
            }

            if (masterInstrument == "MNQ")
            {
                if (setup == SignalSetup.EvidencePullback)
                    return new ValidationProfile(setup, ValidationStage.Candidate, 1.0, true);

                return new ValidationProfile(setup, ValidationStage.Reference, 1.0, false);
            }

            return setup == SignalSetup.EvidencePullback
                ? new ValidationProfile(setup, ValidationStage.Observation, 1.0, true)
                : new ValidationProfile(setup, ValidationStage.Reference, 1.0, false);
        }

        public static ValidationProfile GetPrimaryProfile(string instrument, double configuredTargetR)
        {
            return GetProfile(instrument, SignalSetup.EvidencePullback, configuredTargetR);
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
