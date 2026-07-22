using System;
using NinjaTrader.NinjaScript.TradeAssistant.Models;

namespace NinjaTrader.NinjaScript.TradeAssistant.Configuration
{
    public static class ValidationPlan
    {
        public const string RoundId = "forward-2026-07-v1";
        public const int MinimumSessions = 5;
        public const int MinimumDecidedSignals = 30;

        public static ValidationProfile GetProfile(
            string instrument,
            SignalSetup setup,
            double configuredTargetR)
        {
            string masterInstrument = GetMasterInstrument(instrument);

            if (masterInstrument == "MES")
            {
                if (setup == SignalSetup.EmaCrossBaseline)
                    return new ValidationProfile(setup, ValidationStage.Candidate, 1.0, true);

                return new ValidationProfile(setup, ValidationStage.Paused, 1.0, false);
            }

            if (masterInstrument == "MNQ")
            {
                if (setup == SignalSetup.TrendPullback)
                    return new ValidationProfile(setup, ValidationStage.Observation, 1.5, true);

                return new ValidationProfile(setup, ValidationStage.Reference, 1.0, false);
            }

            return setup == SignalSetup.TrendPullback
                ? new ValidationProfile(setup, ValidationStage.Observation, configuredTargetR, true)
                : new ValidationProfile(setup, ValidationStage.Reference, 1.0, false);
        }

        public static ValidationProfile GetPrimaryProfile(string instrument, double configuredTargetR)
        {
            return GetMasterInstrument(instrument) == "MES"
                ? GetProfile(instrument, SignalSetup.EmaCrossBaseline, configuredTargetR)
                : GetProfile(instrument, SignalSetup.TrendPullback, configuredTargetR);
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
                && NearlyEqual(maximumRiskPerContract, 75.0)
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
