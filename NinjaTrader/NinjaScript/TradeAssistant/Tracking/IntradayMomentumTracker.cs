using System;
using System.Collections.Generic;
using NinjaTrader.NinjaScript.TradeAssistant.Configuration;
using NinjaTrader.NinjaScript.TradeAssistant.Models;

namespace NinjaTrader.NinjaScript.TradeAssistant.Tracking
{
    public sealed class IntradayMomentumTracker
    {
        private readonly Queue<double> openingVolatilityHistory =
            new Queue<double>();
        private readonly Queue<double> highVolatilitySignalHistory =
            new Queue<double>();
        private readonly Queue<double> lowVolatilitySignalHistory =
            new Queue<double>();
        private readonly List<IntradayMomentumTrade> trades =
            new List<IntradayMomentumTrade>();
        private readonly double pointValue;
        private readonly double tickSize;
        private DateTime currentTradingDay = DateTime.MinValue;
        private double currentOpeningVolatility;
        private double firstHalfHourClose;
        private double penultimateHalfHourStart;
        private double previousRegularClose;
        private bool firstHalfHourCaptured;
        private bool openingVolatilityCommitted;
        private bool penultimateStartCaptured;
        private bool signalEvaluated;
        private IntradayMomentumTrade activeTrade;

        public IntradayMomentumTracker(double tickSize, double pointValue)
        {
            this.tickSize = tickSize > 0 ? tickSize : 0.25;
            this.pointValue = pointValue > 0 ? pointValue : 2.0;
        }

        public IntradayMomentumTrade LastTrade
        {
            get { return trades.Count == 0 ? null : trades[trades.Count - 1]; }
        }

        public IList<IntradayMomentumTrade> GetTrades()
        {
            return new List<IntradayMomentumTrade>(trades);
        }

        public IntradayMomentumUpdate Update(
            DateTime time,
            double open,
            double high,
            double low,
            double close,
            double volume)
        {
            DateTime tradingDay = time.Date;
            DateTime regularOpen = IntradayMomentumPlan.GetRegularOpen(tradingDay);
            DateTime regularClose = IntradayMomentumPlan.GetRegularClose(tradingDay);
            if (time <= regularOpen || time > regularClose)
                return IntradayMomentumUpdate.None;

            if (currentTradingDay != tradingDay)
                ResetDay(tradingDay);

            DateTime firstHalfHourEnd = regularOpen.AddMinutes(
                IntradayMomentumPlan.OpeningWindowMinutes);
            DateTime penultimateStart = regularClose.AddHours(-1);
            DateTime lastHalfHourStart = regularClose.AddMinutes(-30);

            if (time <= firstHalfHourEnd)
                currentOpeningVolatility += Math.Max(0, high - low);

            if (!firstHalfHourCaptured && time >= firstHalfHourEnd)
            {
                firstHalfHourClose = close;
                firstHalfHourCaptured = true;
            }

            if (!penultimateStartCaptured && time >= penultimateStart)
            {
                penultimateHalfHourStart = open;
                penultimateStartCaptured = true;
            }

            IntradayMomentumTrade closedTrade = UpdateActiveTrade(
                time,
                high,
                low,
                close,
                regularClose);

            IntradayMomentumTrade openedTrade = null;
            if (!signalEvaluated && time >= lastHalfHourStart)
            {
                signalEvaluated = true;
                openedTrade = TryOpenTrade(
                    tradingDay,
                    time,
                    open,
                    regularClose);
            }

            if (time >= regularClose && !openingVolatilityCommitted)
            {
                previousRegularClose = close;
                CommitOpeningVolatility();
            }

            return openedTrade == null && closedTrade == null
                ? IntradayMomentumUpdate.None
                : new IntradayMomentumUpdate(openedTrade, closedTrade);
        }

        private void ResetDay(DateTime tradingDay)
        {
            currentTradingDay = tradingDay.Date;
            currentOpeningVolatility = 0;
            firstHalfHourClose = 0;
            penultimateHalfHourStart = 0;
            firstHalfHourCaptured = false;
            openingVolatilityCommitted = false;
            penultimateStartCaptured = false;
            signalEvaluated = false;
        }

        private IntradayMomentumTrade TryOpenTrade(
            DateTime tradingDay,
            DateTime signalTime,
            double entryPrice,
            DateTime regularClose)
        {
            if (activeTrade != null
                || previousRegularClose <= 0
                || !firstHalfHourCaptured
                || !penultimateStartCaptured)
            {
                return null;
            }

            double medianOpeningVolatility = Median(openingVolatilityHistory);
            bool highVolatility =
                currentOpeningVolatility >= medianOpeningVolatility;
            double firstHalfHourReturn =
                firstHalfHourClose - previousRegularClose;
            double penultimateReturn =
                entryPrice - penultimateHalfHourStart;
            double highVolatilitySignal = firstHalfHourReturn;
            double lowVolatilitySignal =
                firstHalfHourReturn + penultimateReturn;
            double compositeSignal = highVolatility
                ? highVolatilitySignal
                : lowVolatilitySignal;
            Queue<double> selectedHistory = highVolatility
                ? highVolatilitySignalHistory
                : lowVolatilitySignalHistory;
            bool thresholdReady =
                openingVolatilityHistory.Count
                    >= IntradayMomentumPlan.OpeningVolatilityLookbackSessions
                && selectedHistory.Count
                    >= IntradayMomentumPlan.OpeningVolatilityLookbackSessions
                && StandardDeviation(selectedHistory) > 0;
            AddSignalHistory(
                highVolatilitySignalHistory,
                highVolatilitySignal);
            AddSignalHistory(
                lowVolatilitySignalHistory,
                lowVolatilitySignal);
            if (!thresholdReady
                || Math.Abs(compositeSignal) < tickSize / 2.0)
                return null;

            SignalDirection direction = compositeSignal > 0
                ? SignalDirection.Long
                : SignalDirection.Short;
            double riskPoints =
                IntradayMomentumPlan.RiskCurrencyPerMicro / pointValue;
            double stopPrice = direction == SignalDirection.Long
                ? entryPrice - riskPoints
                : entryPrice + riskPoints;
            entryPrice = RoundToTick(entryPrice);
            stopPrice = RoundToTick(stopPrice);

            activeTrade = new IntradayMomentumTrade(
                "IM-" + tradingDay.ToString("yyyyMMdd"),
                tradingDay,
                highVolatility
                    ? IntradayMomentumRegime.HighOpeningVolatility
                    : IntradayMomentumRegime.LowOpeningVolatility,
                direction,
                signalTime,
                regularClose,
                entryPrice,
                stopPrice,
                tickSize,
                pointValue,
                IntradayMomentumPlan.RiskCurrencyPerMicro,
                IntradayMomentumPlan.RoundTurnCostCurrency,
                currentOpeningVolatility,
                medianOpeningVolatility,
                firstHalfHourReturn,
                penultimateReturn,
                compositeSignal);
            trades.Add(activeTrade);
            return activeTrade;
        }

        private IntradayMomentumTrade UpdateActiveTrade(
            DateTime time,
            double high,
            double low,
            double close,
            DateTime regularClose)
        {
            if (activeTrade == null
                || !activeTrade.IsActive
                || time <= activeTrade.SignalTime)
            {
                return null;
            }

            bool stopHit = activeTrade.Direction == SignalDirection.Long
                ? low <= activeTrade.StopPrice
                : high >= activeTrade.StopPrice;
            if (stopHit)
                return CloseTrade(
                    IntradayMomentumStatus.StopHit,
                    activeTrade.StopPrice,
                    time);

            if (time >= regularClose)
                return CloseTrade(
                    IntradayMomentumStatus.TimeExit,
                    close,
                    time);

            return null;
        }

        private IntradayMomentumTrade CloseTrade(
            IntradayMomentumStatus status,
            double exitPrice,
            DateTime exitTime)
        {
            double grossPoints = activeTrade.Direction == SignalDirection.Long
                ? exitPrice - activeTrade.EntryPrice
                : activeTrade.EntryPrice - exitPrice;
            double resultCurrency =
                grossPoints * pointValue - activeTrade.RoundTurnCost;
            activeTrade.Status = status;
            activeTrade.ExitPrice = RoundToTick(exitPrice);
            activeTrade.ExitTime = exitTime;
            activeTrade.ResultCurrency = Math.Round(resultCurrency, 2);
            activeTrade.ResultR = activeTrade.RiskCurrency <= 0
                ? 0
                : resultCurrency / activeTrade.RiskCurrency;
            IntradayMomentumTrade closedTrade = activeTrade;
            activeTrade = null;
            return closedTrade;
        }

        private void CommitOpeningVolatility()
        {
            openingVolatilityCommitted = true;
            if (currentOpeningVolatility <= 0)
                return;

            openingVolatilityHistory.Enqueue(currentOpeningVolatility);
            while (openingVolatilityHistory.Count
                > IntradayMomentumPlan.OpeningVolatilityLookbackSessions)
            {
                openingVolatilityHistory.Dequeue();
            }
        }

        private double RoundToTick(double price)
        {
            return Math.Round(
                price / tickSize,
                MidpointRounding.AwayFromZero) * tickSize;
        }

        private static double Median(IEnumerable<double> values)
        {
            List<double> ordered = new List<double>(values);
            ordered.Sort();
            int middle = ordered.Count / 2;
            return ordered.Count % 2 == 0
                ? (ordered[middle - 1] + ordered[middle]) / 2.0
                : ordered[middle];
        }

        private static void AddSignalHistory(
            Queue<double> history,
            double value)
        {
            history.Enqueue(value);
            while (history.Count
                > IntradayMomentumPlan.OpeningVolatilityLookbackSessions)
            {
                history.Dequeue();
            }
        }

        private static double StandardDeviation(
            IEnumerable<double> values)
        {
            List<double> samples = new List<double>(values);
            if (samples.Count == 0)
                return 0;

            double total = 0;
            foreach (double value in samples)
                total += value;
            double mean = total / samples.Count;
            double squaredDifferences = 0;
            foreach (double value in samples)
            {
                double difference = value - mean;
                squaredDifferences += difference * difference;
            }
            return Math.Sqrt(squaredDifferences / samples.Count);
        }
    }
}
