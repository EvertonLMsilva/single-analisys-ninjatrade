using System;
using System.Collections.Generic;

namespace NinjaTrader.NinjaScript.TradeAssistant.MarketData
{
    public sealed class MarketDeltaTracker
    {
        private readonly object syncRoot;
        private readonly int periodMinutes;
        private readonly double tickSize;

        private readonly Dictionary<DateTime, MutableDeltaBar> bars;
        private readonly Queue<DateTime> barOrder;

        private double previousTradePrice;
        private int previousTradeDirection;

        public MarketDeltaTracker(
            int periodMinutes,
            double tickSize)
        {
            this.periodMinutes = Math.Max(1, periodMinutes);
            this.tickSize = tickSize > 0 ? tickSize : 0.0000001;

            syncRoot = new object();
            bars = new Dictionary<DateTime, MutableDeltaBar>();
            barOrder = new Queue<DateTime>();

            previousTradePrice = double.NaN;
            previousTradeDirection = 0;
        }

        public void ProcessTrade(
            DateTime eventTime,
            double tradePrice,
            long volume,
            double bidPrice,
            double askPrice)
        {
            if (volume <= 0 || tradePrice <= 0)
                return;

            DateTime barTime = GetBarEndTime(
                eventTime,
                periodMinutes);

            lock (syncRoot)
            {
                MutableDeltaBar bar;
                if (!bars.TryGetValue(barTime, out bar))
                {
                    bar = new MutableDeltaBar();
                    bars.Add(barTime, bar);
                    barOrder.Enqueue(barTime);

                    RemoveOldBars();
                }

                int direction = ClassifyTrade(
                    tradePrice,
                    bidPrice,
                    askPrice);

                if (direction > 0)
                    bar.BuyVolume += volume;
                else if (direction < 0)
                    bar.SellVolume += volume;
                else
                    bar.UnknownVolume += volume;

                previousTradePrice = tradePrice;

                if (direction != 0)
                    previousTradeDirection = direction;
            }
        }

        public MarketDeltaSnapshot GetSnapshot(
            DateTime barTime)
        {
            lock (syncRoot)
            {
                MutableDeltaBar bar;
                if (!bars.TryGetValue(barTime, out bar))
                    return null;

                return new MarketDeltaSnapshot(
                    barTime,
                    bar.BuyVolume,
                    bar.SellVolume,
                    bar.UnknownVolume);
            }
        }

        public void Reset()
        {
            lock (syncRoot)
            {
                bars.Clear();
                barOrder.Clear();

                previousTradePrice = double.NaN;
                previousTradeDirection = 0;
            }
        }

        private int ClassifyTrade(
            double tradePrice,
            double bidPrice,
            double askPrice)
        {
            double tolerance = Math.Max(
                tickSize * 0.1,
                0.0000001);

            if (askPrice > 0
                && tradePrice >= askPrice - tolerance)
            {
                return 1;
            }

            if (bidPrice > 0
                && tradePrice <= bidPrice + tolerance)
            {
                return -1;
            }

            if (!double.IsNaN(previousTradePrice))
            {
                if (tradePrice > previousTradePrice)
                    return 1;

                if (tradePrice < previousTradePrice)
                    return -1;
            }

            return previousTradeDirection;
        }

        private void RemoveOldBars()
        {
            const int maximumStoredBars = 20;

            while (barOrder.Count > maximumStoredBars)
            {
                DateTime oldestBarTime =
                    barOrder.Dequeue();

                bars.Remove(oldestBarTime);
            }
        }

        private static DateTime GetBarEndTime(
            DateTime eventTime,
            int minutes)
        {
            DateTime minuteTime = new DateTime(
                eventTime.Year,
                eventTime.Month,
                eventTime.Day,
                eventTime.Hour,
                eventTime.Minute,
                0,
                eventTime.Kind);

            int minuteOfDay =
                minuteTime.Hour * 60
                + minuteTime.Minute;

            int remainder =
                minuteOfDay % minutes;

            int minutesToEnd =
                remainder == 0
                    ? minutes
                    : minutes - remainder;

            return minuteTime.AddMinutes(minutesToEnd);
        }

        private sealed class MutableDeltaBar
        {
            public long BuyVolume;
            public long SellVolume;
            public long UnknownVolume;
        }
    }
}