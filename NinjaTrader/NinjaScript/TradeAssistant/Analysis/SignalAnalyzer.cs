using System;
using NinjaTrader.NinjaScript.TradeAssistant.Models;

namespace NinjaTrader.NinjaScript.TradeAssistant.Analysis
{
    public sealed class SignalAnalyzer
    {
        public TradeSignal CreateEmaCross(
            SignalDirection direction,
            double entryPrice,
            double atr,
            double stopAtrMultiplier,
            double riskRewardRatio,
            double tickSize,
            double pointValue,
            DateTime createdAt,
            int validForBars)
        {
            double normalizedTickSize = tickSize > 0 ? tickSize : 1;
            double normalizedEntryPrice = RoundToTickSize(entryPrice, normalizedTickSize);
            double risk = atr * stopAtrMultiplier;
            double stopPrice = direction == SignalDirection.Long
                ? normalizedEntryPrice - risk
                : normalizedEntryPrice + risk;
            stopPrice = RoundToTickSize(stopPrice, normalizedTickSize);

            if (direction == SignalDirection.Long && stopPrice >= normalizedEntryPrice)
                stopPrice = normalizedEntryPrice - normalizedTickSize;
            else if (direction == SignalDirection.Short && stopPrice <= normalizedEntryPrice)
                stopPrice = normalizedEntryPrice + normalizedTickSize;

            double normalizedRisk = Math.Abs(normalizedEntryPrice - stopPrice);
            double targetPrice = direction == SignalDirection.Long
                ? normalizedEntryPrice + (normalizedRisk * riskRewardRatio)
                : normalizedEntryPrice - (normalizedRisk * riskRewardRatio);
            targetPrice = RoundToTickSize(targetPrice, normalizedTickSize);

            string reason = direction == SignalDirection.Long
                ? "EMA rápida cruzou acima da EMA lenta"
                : "EMA rápida cruzou abaixo da EMA lenta";

            return new TradeSignal(
                Guid.NewGuid().ToString("N"),
                SignalSetup.EmaCrossBaseline,
                direction,
                normalizedEntryPrice,
                stopPrice,
                targetPrice,
                normalizedTickSize,
                pointValue,
                createdAt,
                validForBars,
                1,
                reason);
        }

        public TradeSignal CreatePullback(
            SignalDirection direction,
            double entryPrice,
            double technicalStopPrice,
            double riskRewardRatio,
            double tickSize,
            double pointValue,
            DateTime createdAt,
            int validForBars)
        {
            double normalizedTickSize = tickSize > 0 ? tickSize : 1;
            double normalizedEntryPrice = RoundToTickSize(entryPrice, normalizedTickSize);
            double stopPrice = RoundToTickSize(technicalStopPrice, normalizedTickSize);

            if (direction == SignalDirection.Long && stopPrice >= normalizedEntryPrice)
                stopPrice = normalizedEntryPrice - normalizedTickSize;
            else if (direction == SignalDirection.Short && stopPrice <= normalizedEntryPrice)
                stopPrice = normalizedEntryPrice + normalizedTickSize;

            double risk = Math.Abs(normalizedEntryPrice - stopPrice);
            double targetPrice = direction == SignalDirection.Long
                ? normalizedEntryPrice + (risk * riskRewardRatio)
                : normalizedEntryPrice - (risk * riskRewardRatio);
            targetPrice = RoundToTickSize(targetPrice, normalizedTickSize);

            string reason = direction == SignalDirection.Long
                ? "Pullback na EMA rapida com confirmacao compradora"
                : "Pullback na EMA rapida com confirmacao vendedora";

            return new TradeSignal(
                Guid.NewGuid().ToString("N"),
                SignalSetup.TrendPullback,
                direction,
                normalizedEntryPrice,
                stopPrice,
                targetPrice,
                normalizedTickSize,
                pointValue,
                createdAt,
                validForBars,
                2,
                reason);
        }

        private static double RoundToTickSize(double price, double tickSize)
        {
            return Math.Round(price / tickSize, MidpointRounding.AwayFromZero) * tickSize;
        }
    }
}
