using System;

namespace NinjaTrader.NinjaScript.TradeAssistant.Models
{
    public sealed class TradeSignal
    {
        public TradeSignal(
            string id,
            SignalSetup setup,
            SignalDirection direction,
            double entryPrice,
            double stopPrice,
            double targetPrice,
            double tickSize,
            double pointValue,
            DateTime createdAt,
            int validForBars,
            int score,
            string reason)
            : this(
                id,
                setup,
                direction,
                entryPrice,
                stopPrice,
                targetPrice,
                tickSize,
                pointValue,
                createdAt,
                validForBars,
                score,
                reason,
                SignalContext.Empty)
        {
        }

        public TradeSignal(
            string id,
            SignalSetup setup,
            SignalDirection direction,
            double entryPrice,
            double stopPrice,
            double targetPrice,
            double tickSize,
            double pointValue,
            DateTime createdAt,
            int validForBars,
            int score,
            string reason,
            SignalContext context)
        {
            Id = id;
            Setup = setup;
            Direction = direction;
            EntryPrice = entryPrice;
            StopPrice = stopPrice;
            TargetPrice = targetPrice;
            TickSize = tickSize;
            PointValue = pointValue;
            CreatedAt = createdAt;
            ValidForBars = validForBars;
            Score = score;
            Reason = reason;
            Context = context ?? SignalContext.Empty;
        }

        public string Id { get; private set; }
        public SignalSetup Setup { get; private set; }
        public SignalDirection Direction { get; private set; }
        public double EntryPrice { get; private set; }
        public double StopPrice { get; private set; }
        public double TargetPrice { get; private set; }
        public double TickSize { get; private set; }
        public double PointValue { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public int ValidForBars { get; private set; }
        public int Score { get; private set; }
        public string Reason { get; private set; }
        public SignalContext Context { get; private set; }

        public double Risk
        {
            get { return Math.Abs(EntryPrice - StopPrice); }
        }

        public double RiskRewardRatio
        {
            get { return Risk == 0 ? 0 : Math.Abs(TargetPrice - EntryPrice) / Risk; }
        }

        public double RiskTicks
        {
            get { return TickSize <= 0 ? 0 : Risk / TickSize; }
        }

        public double RiskCurrency
        {
            get { return Risk * PointValue; }
        }

        public double RewardCurrency
        {
            get { return Math.Abs(TargetPrice - EntryPrice) * PointValue; }
        }

        public double TargetOneRPrice
        {
            get { return GetTargetPrice(1.0); }
        }

        public double TargetOnePointFiveRPrice
        {
            get { return GetTargetPrice(1.5); }
        }

        public double TargetTwoRPrice
        {
            get { return GetTargetPrice(2.0); }
        }

        private double GetTargetPrice(double riskMultiple)
        {
            double rawPrice = Direction == SignalDirection.Long
                ? EntryPrice + (Risk * riskMultiple)
                : EntryPrice - (Risk * riskMultiple);

            if (TickSize <= 0)
                return rawPrice;

            return Math.Round(rawPrice / TickSize, MidpointRounding.AwayFromZero) * TickSize;
        }
    }
}
