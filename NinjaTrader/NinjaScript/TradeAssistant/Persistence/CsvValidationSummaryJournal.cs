using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using NinjaTrader.NinjaScript.TradeAssistant.Analysis;
using NinjaTrader.NinjaScript.TradeAssistant.Configuration;
using NinjaTrader.NinjaScript.TradeAssistant.Models;

namespace NinjaTrader.NinjaScript.TradeAssistant.Persistence
{
    public sealed class CsvValidationSummaryJournal
    {
        private const string Header = "Date,Instrument,BarsPeriod,Version,ValidationRound,ValidationSample,EligibleForReview,Setup,ValidationStage,ValidationTargetR,MinimumSessions,MinimumDecidedSignals,Total,Active,Targets,Stops,Expired,Ambiguous,RiskRejected,Decided,WinRate,ResultR,ResultCurrency,Currency,AverageWinnerRiskCurrency,AverageLoserRiskCurrency,MaximumConsecutiveLosses,MaximumDrawdownR,MaximumDrawdownCurrency,CostsIncluded";
        private static readonly object FileLock = new object();

        private readonly string barsPeriod;
        private readonly string currency;
        private readonly string directory;
        private readonly string instrument;
        private readonly double configuredTargetR;
        private readonly string version;

        public CsvValidationSummaryJournal(
            string directory,
            string instrument,
            string barsPeriod,
            string version,
            string currency,
            double configuredTargetR)
        {
            this.directory = directory;
            this.instrument = instrument;
            this.barsPeriod = barsPeriod;
            this.version = version;
            this.currency = currency;
            this.configuredTargetR = configuredTargetR;
        }

        public string LastError { get; private set; }

        public bool Record(IList<TrackedSignal> signals)
        {
            try
            {
                SortedSet<DateTime> days = new SortedSet<DateTime>();
                foreach (TrackedSignal signal in signals)
                    days.Add(signal.Signal.CreatedAt.Date);

                lock (FileLock)
                {
                    Directory.CreateDirectory(directory);
                    foreach (DateTime day in days)
                        WriteDay(day, signals);
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

        private void WriteDay(DateTime day, IList<TrackedSignal> signals)
        {
            List<string> lines = new List<string>();
            lines.Add(Header);

            foreach (SignalSetup setup in new[]
            {
                SignalSetup.EmaCrossBaseline,
                SignalSetup.TrendPullback,
                SignalSetup.ContextPullback,
                SignalSetup.EvidencePullback,
                SignalSetup.QualifiedPullback
            })
            {
                bool hasSetup = false;
                foreach (TrackedSignal signal in signals)
                {
                    if (signal.Signal.Setup == setup && signal.Signal.CreatedAt.Date == day.Date)
                    {
                        hasSetup = true;
                        break;
                    }
                }

                if (!hasSetup)
                    continue;

                ValidationProfile profile = ValidationPlan.GetProfile(instrument, setup, configuredTargetR);
                ValidationStatistics statistics = ValidationStatisticsCalculator.Calculate(
                    signals,
                    setup,
                    profile.TargetR,
                    day);
                lines.Add(BuildRow(day, profile, statistics));
            }

            File.WriteAllLines(GetFilePath(day), lines.ToArray(), new UTF8Encoding(true));
        }

        private string BuildRow(
            DateTime day,
            ValidationProfile profile,
            ValidationStatistics statistics)
        {
            return string.Join(
                ",",
                Csv(day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                Csv(instrument),
                Csv(barsPeriod),
                Csv(version),
                Csv(ValidationPlan.RoundId),
                Csv(ValidationPlan.GetSamplePhase(day)),
                Csv(ValidationPlan.IsForwardSample(day) ? "Yes" : "No"),
                Csv(profile.Setup.ToString()),
                Csv(profile.Stage.ToString()),
                Number(profile.TargetR),
                ValidationPlan.MinimumSessions.ToString(CultureInfo.InvariantCulture),
                ValidationPlan.MinimumDecidedSignals.ToString(CultureInfo.InvariantCulture),
                statistics.Total.ToString(CultureInfo.InvariantCulture),
                statistics.Active.ToString(CultureInfo.InvariantCulture),
                statistics.TargetHits.ToString(CultureInfo.InvariantCulture),
                statistics.StopHits.ToString(CultureInfo.InvariantCulture),
                statistics.Expired.ToString(CultureInfo.InvariantCulture),
                statistics.Ambiguous.ToString(CultureInfo.InvariantCulture),
                statistics.RiskRejected.ToString(CultureInfo.InvariantCulture),
                statistics.Decided.ToString(CultureInfo.InvariantCulture),
                Number(statistics.WinRate),
                Number(statistics.ResultR),
                Number(statistics.ResultCurrency),
                Csv(currency),
                Number(statistics.AverageWinnerRiskCurrency),
                Number(statistics.AverageLoserRiskCurrency),
                statistics.MaximumConsecutiveLosses.ToString(CultureInfo.InvariantCulture),
                Number(statistics.MaximumDrawdownR),
                Number(statistics.MaximumDrawdownCurrency),
                Csv("No"));
        }

        private string GetFilePath(DateTime day)
        {
            string fileName = string.Format(
                CultureInfo.InvariantCulture,
                "{0}_{1}_{2}_validation_v3.csv",
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
