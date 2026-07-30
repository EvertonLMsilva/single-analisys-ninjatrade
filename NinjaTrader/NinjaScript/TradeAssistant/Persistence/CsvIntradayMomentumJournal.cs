using System;
using System.Globalization;
using System.IO;
using System.Text;
using NinjaTrader.NinjaScript.TradeAssistant.Configuration;
using NinjaTrader.NinjaScript.TradeAssistant.Models;

namespace NinjaTrader.NinjaScript.TradeAssistant.Persistence
{
    public sealed class CsvIntradayMomentumJournal
    {
        private const string Header = "RecordKey,TradingDay,Instrument,BarsPeriod,Version,Round,Sample,EligibleForReview,Setup,Regime,Direction,SignalTime,ScheduledExitTime,ExitTime,EntryPrice,StopPrice,ExitPrice,TickSize,PointValue,RiskCurrency,RoundTurnCost,Status,ResultCurrency,ResultR,OpeningVolatility,MedianOpeningVolatility,FirstHalfHourReturn,PenultimateHalfHourReturn,CompositeSignal";
        private static readonly object FileLock = new object();

        private readonly string barsPeriod;
        private readonly string directory;
        private readonly string instrument;
        private readonly string version;

        public CsvIntradayMomentumJournal(
            string directory,
            string instrument,
            string barsPeriod,
            string version)
        {
            this.directory = directory;
            this.instrument = instrument;
            this.barsPeriod = barsPeriod;
            this.version = version;
        }

        public string LastError { get; private set; }

        public bool Record(IntradayMomentumTrade trade)
        {
            if (trade == null)
                return true;

            try
            {
                lock (FileLock)
                {
                    Directory.CreateDirectory(directory);
                    File.WriteAllLines(
                        GetFilePath(trade.TradingDay),
                        new[] { Header, BuildRow(trade) },
                        new UTF8Encoding(true));
                }

                LastError = null;
                return true;
            }
            catch (Exception exception)
            {
                LastError = exception.Message;
                return false;
            }
        }

        public string GetDirectory()
        {
            return directory;
        }

        private string BuildRow(IntradayMomentumTrade trade)
        {
            return string.Join(
                ",",
                Csv(trade.Id),
                Csv(trade.TradingDay.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                Csv(instrument),
                Csv(barsPeriod),
                Csv(version),
                Csv(IntradayMomentumPlan.RoundId),
                Csv(IntradayMomentumPlan.GetSamplePhase(trade.TradingDay)),
                Csv(IntradayMomentumPlan.IsProspective(trade.TradingDay) ? "Yes" : "No"),
                Csv("IntradayMomentum"),
                Csv(trade.Regime.ToString()),
                Csv(trade.Direction.ToString()),
                Csv(DateTimeValue(trade.SignalTime)),
                Csv(DateTimeValue(trade.ScheduledExitTime)),
                Csv(trade.ExitTime.HasValue ? DateTimeValue(trade.ExitTime.Value) : string.Empty),
                Number(trade.EntryPrice),
                Number(trade.StopPrice),
                NullableNumber(trade.ExitPrice),
                Number(trade.TickSize),
                Number(trade.PointValue),
                Number(trade.RiskCurrency),
                Number(trade.RoundTurnCost),
                Csv(trade.Status.ToString()),
                Number(trade.ResultCurrency),
                Number(trade.ResultR),
                Number(trade.OpeningVolatility),
                Number(trade.MedianOpeningVolatility),
                Number(trade.FirstHalfHourReturn),
                Number(trade.PenultimateHalfHourReturn),
                Number(trade.CompositeSignal));
        }

        private string GetFilePath(DateTime day)
        {
            string fileName = string.Format(
                CultureInfo.InvariantCulture,
                "{0}_{1}_{2}_intraday_momentum_v1.csv",
                day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Sanitize(instrument),
                Sanitize(barsPeriod));
            return Path.Combine(directory, fileName);
        }

        private static string Csv(string value)
        {
            string safeValue = value ?? string.Empty;
            if (safeValue.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0)
                return safeValue;
            return "\"" + safeValue.Replace("\"", "\"\"") + "\"";
        }

        private static string DateTimeValue(DateTime value)
        {
            return value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }

        private static string NullableNumber(double? value)
        {
            return value.HasValue ? Number(value.Value) : string.Empty;
        }

        private static string Number(double value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static string Sanitize(string value)
        {
            StringBuilder builder = new StringBuilder();
            foreach (char character in value ?? string.Empty)
                builder.Append(char.IsLetterOrDigit(character) || character == '-' ? character : '_');
            return builder.Length == 0 ? "unknown" : builder.ToString();
        }
    }
}
