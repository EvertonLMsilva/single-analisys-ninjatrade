using System;
using NinjaTrader.NinjaScript.TradeAssistant.Models;

namespace NinjaTrader.NinjaScript.TradeAssistant.Analysis
{
    public sealed class SignalAnalyzer
    {
        public TradeSignal Create(
            SignalDirection direction,
            double entryPrice,
            double atr,
            double stopAtrMultiplier,
            double riskRewardRatio,
            DateTime createdAt,
            int validForBars)
        {
            double risk = atr * stopAtrMultiplier;
            double stopPrice = direction == SignalDirection.Long
                ? entryPrice - risk
                : entryPrice + risk;
            double targetPrice = direction == SignalDirection.Long
                ? entryPrice + (risk * riskRewardRatio)
                : entryPrice - (risk * riskRewardRatio);

            string reason = direction == SignalDirection.Long
                ? "EMA rápida cruzou acima da EMA lenta"
                : "EMA rápida cruzou abaixo da EMA lenta";

            return new TradeSignal(
                Guid.NewGuid().ToString("N"),
                direction,
                entryPrice,
                stopPrice,
                targetPrice,
                createdAt,
                validForBars,
                1,
                reason);
        }
    }
}
