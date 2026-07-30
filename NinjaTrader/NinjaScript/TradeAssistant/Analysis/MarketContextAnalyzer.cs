using System;
using System.Collections.Generic;
using NinjaTrader.NinjaScript.TradeAssistant.Models;

namespace NinjaTrader.NinjaScript.TradeAssistant.Analysis
{
    public sealed class MarketContextAnalyzer
    {
        public const double MaximumVwapDistanceAtr = 1.25;
        public const double MinimumCandleBodyAtr = 0.15;
        public const double MinimumRelativeVolume = 0.8;
        public const double LongMinimumCloseLocation = 0.65;
        public const double ShortMaximumCloseLocation = 0.35;
        public const int MinimumScore = 5;

        public SignalContext Analyze(
            SignalDirection direction,
            double close,
            double sessionVwap,
            double previousSessionVwap,
            double fastEmaSlopeAtr,
            double slowEmaSlopeAtr,
            double sessionHigh,
            double sessionLow,
            double candleBodyAtr,
            double closeLocation,
            double relativeVolume,
            double atr)
        {
            double normalizedAtr = atr > 0 ? atr : 1;
            double vwapSlopeAtr = (sessionVwap - previousSessionVwap) / normalizedAtr;
            double vwapDistanceAtr = Math.Abs(close - sessionVwap) / normalizedAtr;
            bool isLong = direction == SignalDirection.Long;
            bool correctVwapSide = isLong ? close > sessionVwap : close < sessionVwap;
            bool vwapAligned = isLong ? vwapSlopeAtr > 0 : vwapSlopeAtr < 0;
            bool emaSlopeAligned = isLong
                ? fastEmaSlopeAtr > 0 && slowEmaSlopeAtr >= 0
                : fastEmaSlopeAtr < 0 && slowEmaSlopeAtr <= 0;
            bool candleConfirmed = candleBodyAtr >= MinimumCandleBodyAtr
                && (isLong
                    ? closeLocation >= LongMinimumCloseLocation
                    : closeLocation <= ShortMaximumCloseLocation);
            bool notExtended = vwapDistanceAtr <= MaximumVwapDistanceAtr;
            bool volumeConfirmed = relativeVolume >= MinimumRelativeVolume;

            int score = 0;
            List<string> confirmations = new List<string>();
            Add(confirmations, correctVwapSide, "lado VWAP", ref score);
            Add(confirmations, vwapAligned, "VWAP inclinada", ref score);
            Add(confirmations, emaSlopeAligned, "EMAs alinhadas", ref score);
            Add(confirmations, candleConfirmed, "candle forte", ref score);
            Add(confirmations, notExtended, "sem extensao", ref score);
            Add(confirmations, volumeConfirmed, "volume", ref score);

            bool passed = correctVwapSide
                && vwapAligned
                && emaSlopeAligned
                && candleConfirmed
                && notExtended
                && score >= MinimumScore;
            string summary = string.Join(" | ", confirmations.ToArray())
                + " | score " + score + "/6"
                + (passed ? " | APROVADO" : " | REPROVADO");

            return new SignalContext(
                sessionVwap,
                vwapSlopeAtr,
                vwapDistanceAtr,
                fastEmaSlopeAtr,
                slowEmaSlopeAtr,
                sessionHigh,
                sessionLow,
                candleBodyAtr,
                closeLocation,
                relativeVolume,
                score,
                passed,
                summary);
        }

        private static void Add(
            ICollection<string> confirmations,
            bool passed,
            string name,
            ref int score)
        {
            if (passed)
                score++;
            confirmations.Add((passed ? "+" : "-") + name);
        }
    }
}
