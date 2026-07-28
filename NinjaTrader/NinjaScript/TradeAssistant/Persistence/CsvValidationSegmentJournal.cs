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
    public sealed class CsvValidationSegmentJournal
    {
        private const string Header = "Date,Instrument,BarsPeriod,Version,ValidationRound,ValidationSample,EligibleForReview,Setup,ValidationStage,ValidationTargetR,Dimension,Segment,Total,Active,Targets,Stops,Expired,Ambiguous,RiskRejected,Decided,WinRate,ResultR,ResultCurrency,Currency,AverageWinnerRiskCurrency,AverageLoserRiskCurrency,CostsIncluded";
        private static readonly object FileLock = new object();

        private readonly string barsPeriod;
        private readonly string currency;
        private readonly string directory;
        private readonly string instrument;
        private readonly double configuredTargetR;
        private readonly string version;

        public CsvValidationSegmentJournal(
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

        private void WriteDay(DateTime day, IList<TrackedSignal> signals)
        {
            List<string> lines = new List<string>();
            lines.Add(Header);

            foreach (SignalSetup setup in new[]
            {
                SignalSetup.EmaCrossBaseline,
                SignalSetup.TrendPullback,
                SignalSetup.ContextPullback
            })
            {
                List<TrackedSignal> setupSignals = Filter(signals, day, setup, null, null, null);
                if (setupSignals.Count == 0)
                    continue;

                ValidationProfile profile = ValidationPlan.GetProfile(instrument, setup, configuredTargetR);

                foreach (SignalDirection direction in new[] { SignalDirection.Long, SignalDirection.Short })
                    AddSegment(lines, day, profile, "Direction", direction.ToString(),
                        Filter(signals, day, setup, direction, null, null));

                SortedSet<int> hours = new SortedSet<int>();
                foreach (TrackedSignal signal in setupSignals)
                    hours.Add(signal.Signal.CreatedAt.Hour);
                foreach (int hour in hours)
                    AddSegment(lines, day, profile, "Hour",
                        hour.ToString("00", CultureInfo.InvariantCulture) + ":00",
                        Filter(signals, day, setup, null, hour, null));

                foreach (string riskBand in new[] { "0-25", "25-50", "50-75", "75+" })
                    AddSegment(lines, day, profile, "RiskBand", riskBand,
                        Filter(signals, day, setup, null, null, riskBand));

                SortedSet<int> contextScores = new SortedSet<int>();
                foreach (TrackedSignal signal in setupSignals)
                    contextScores.Add(signal.Signal.Context.Score);
                foreach (int contextScore in contextScores)
                    AddSegment(lines, day, profile, "ContextScore",
                        contextScore.ToString(CultureInfo.InvariantCulture),
                        FilterByContextScore(setupSignals, contextScore));
            }

            File.WriteAllLines(GetFilePath(day), lines.ToArray(), new UTF8Encoding(true));
        }

        private void AddSegment(
            ICollection<string> lines,
            DateTime day,
            ValidationProfile profile,
            string dimension,
            string segment,
            IList<TrackedSignal> signals)
        {
            if (signals.Count == 0)
                return;

            ValidationStatistics statistics = ValidationStatisticsCalculator.Calculate(
                signals,
                profile.Setup,
                profile.TargetR,
                day);

            lines.Add(string.Join(
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
                Csv(dimension),
                Csv(segment),
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
                Csv("No")));
        }

        private static List<TrackedSignal> Filter(
            IEnumerable<TrackedSignal> signals,
            DateTime day,
            SignalSetup setup,
            SignalDirection? direction,
            int? hour,
            string riskBand)
        {
            List<TrackedSignal> filtered = new List<TrackedSignal>();
            foreach (TrackedSignal signal in signals)
            {
                if (signal.Signal.CreatedAt.Date != day.Date
                    || signal.Signal.Setup != setup
                    || (direction.HasValue && signal.Signal.Direction != direction.Value)
                    || (hour.HasValue && signal.Signal.CreatedAt.Hour != hour.Value)
                    || (riskBand != null && GetRiskBand(signal.Signal.RiskCurrency) != riskBand))
                    continue;

                filtered.Add(signal);
            }

            return filtered;
        }

        private static List<TrackedSignal> FilterByContextScore(
            IEnumerable<TrackedSignal> signals,
            int contextScore)
        {
            List<TrackedSignal> filtered = new List<TrackedSignal>();
            foreach (TrackedSignal signal in signals)
            {
                if (signal.Signal.Context.Score == contextScore)
                    filtered.Add(signal);
            }

            return filtered;
        }

        private string GetFilePath(DateTime day)
        {
            string fileName = string.Format(
                CultureInfo.InvariantCulture,
                "{0}_{1}_{2}_segments_v2.csv",
                day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Sanitize(instrument),
                Sanitize(barsPeriod));
            return Path.Combine(directory, fileName);
        }

        private static string GetRiskBand(double riskCurrency)
        {
            if (riskCurrency <= 25)
                return "0-25";
            if (riskCurrency <= 50)
                return "25-50";
            if (riskCurrency <= 75)
                return "50-75";
            return "75+";
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
