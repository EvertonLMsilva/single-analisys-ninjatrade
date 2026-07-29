using System;

namespace NinjaTrader.NinjaScript.TradeAssistant.Models
{
    public sealed class IntradayMomentumTrade
    {
        public IntradayMomentumTrade(
            string id,
            DateTime tradingDay,
            IntradayMomentumRegime regime,
            SignalDirection direction,
            DateTime signalTime,
            DateTime scheduledExitTime,
            double entryPrice,
            double stopPrice,
            double tickSize,
            double pointValue,
            double riskCurrency,
            double roundTurnCost,
            double openingVolatility,
            double medianOpeningVolatility,
            double firstHalfHourReturn,
            double penultimateHalfHourReturn,
            double compositeSignal)
        {
            Id = id;
            TradingDay = tradingDay.Date;
            Regime = regime;
            Direction = direction;
            SignalTime = signalTime;
            ScheduledExitTime = scheduledExitTime;
            EntryPrice = entryPrice;
            StopPrice = stopPrice;
            TickSize = tickSize;
            PointValue = pointValue;
            RiskCurrency = riskCurrency;
            RoundTurnCost = roundTurnCost;
            OpeningVolatility = openingVolatility;
            MedianOpeningVolatility = medianOpeningVolatility;
            FirstHalfHourReturn = firstHalfHourReturn;
            PenultimateHalfHourReturn = penultimateHalfHourReturn;
            CompositeSignal = compositeSignal;
            Status = IntradayMomentumStatus.Active;
        }

        public string Id { get; private set; }
        public DateTime TradingDay { get; private set; }
        public IntradayMomentumRegime Regime { get; private set; }
        public SignalDirection Direction { get; private set; }
        public DateTime SignalTime { get; private set; }
        public DateTime ScheduledExitTime { get; private set; }
        public DateTime? ExitTime { get; internal set; }
        public double EntryPrice { get; private set; }
        public double StopPrice { get; private set; }
        public double? ExitPrice { get; internal set; }
        public double TickSize { get; private set; }
        public double PointValue { get; private set; }
        public double RiskCurrency { get; private set; }
        public double RoundTurnCost { get; private set; }
        public double OpeningVolatility { get; private set; }
        public double MedianOpeningVolatility { get; private set; }
        public double FirstHalfHourReturn { get; private set; }
        public double PenultimateHalfHourReturn { get; private set; }
        public double CompositeSignal { get; private set; }
        public IntradayMomentumStatus Status { get; internal set; }
        public double ResultCurrency { get; internal set; }
        public double ResultR { get; internal set; }

        public bool IsActive
        {
            get { return Status == IntradayMomentumStatus.Active; }
        }
    }
}
