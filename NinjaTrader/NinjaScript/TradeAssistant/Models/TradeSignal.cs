using System;

namespace NinjaTrader.NinjaScript.TradeAssistant.Models
{
    public sealed class TradeSignal
    {
        public TradeSignal(
            string id,
            SignalDirection direction,
            double entryPrice,
            double stopPrice,
            double targetPrice,
            DateTime createdAt,
            int validForBars,
            int score,
            string reason)
        {
            Id = id;
            Direction = direction;
            EntryPrice = entryPrice;
            StopPrice = stopPrice;
            TargetPrice = targetPrice;
            CreatedAt = createdAt;
            ValidForBars = validForBars;
            Score = score;
            Reason = reason;
        }

        public string Id { get; private set; }
        public SignalDirection Direction { get; private set; }
        public double EntryPrice { get; private set; }
        public double StopPrice { get; private set; }
        public double TargetPrice { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public int ValidForBars { get; private set; }
        public int Score { get; private set; }
        public string Reason { get; private set; }

        public double Risk
        {
            get { return Math.Abs(EntryPrice - StopPrice); }
        }

        public double RiskRewardRatio
        {
            get { return Risk == 0 ? 0 : Math.Abs(TargetPrice - EntryPrice) / Risk; }
        }
    }
}
