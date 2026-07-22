using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using NinjaTrader.NinjaScript.TradeAssistant.Configuration;
using NinjaTrader.NinjaScript.TradeAssistant.Models;

namespace NinjaTrader.NinjaScript.TradeAssistant.Persistence
{
    public sealed class CsvSignalJournal
    {
        private const string Header = "RecordKey,SignalId,SignalTime,ClosedAt,Instrument,BarsPeriod,Version,EvaluationType,EntryAssumption,OutcomeBasis,ValidationRound,ValidationStage,ValidationTargetR,ValidationSample,Setup,Direction,EntryPrice,StopPrice,TargetPrice,TickSize,PointValue,Currency,RiskPoints,RiskTicks,RiskCurrency,RewardCurrency,MaximumRiskPerContract,RiskLimitMode,RiskLimitStatus,RiskReward,ValidForBars,FastEmaPeriod,SlowEmaPeriod,AtrPeriod,StopAtrMultiplier,PullbackToleranceAtr,PullbackCooldownBars,Status,ResultR,MfeR,MaeR,BarsElapsed,FirstEvent,FirstEventAt,Target1RPrice,Target1RStatus,Target1RAt,Target1_5RPrice,Target1_5RStatus,Target1_5RAt,Target2RPrice,Target2RStatus,Target2RAt,Reason";
        private static readonly object FileLock = new object();

        private readonly int atrPeriod;
        private readonly string barsPeriod;
        private readonly string directory;
        private readonly int fastEmaPeriod;
        private readonly string instrument;
        private readonly string currency;
        private readonly double maximumRiskPerContract;
        private readonly int pullbackCooldownBars;
        private readonly double pullbackToleranceAtr;
        private readonly string riskLimitMode;
        private readonly int slowEmaPeriod;
        private readonly double stopAtrMultiplier;
        private readonly string version;

        public CsvSignalJournal(
            string directory,
            string instrument,
            string barsPeriod,
            string version,
            int fastEmaPeriod,
            int slowEmaPeriod,
            int atrPeriod,
            double stopAtrMultiplier,
            double pullbackToleranceAtr,
            int pullbackCooldownBars,
            string currency,
            double maximumRiskPerContract,
            string riskLimitMode)
        {
            this.directory = directory;
            this.instrument = instrument;
            this.barsPeriod = barsPeriod;
            this.version = version;
            this.fastEmaPeriod = fastEmaPeriod;
            this.slowEmaPeriod = slowEmaPeriod;
            this.atrPeriod = atrPeriod;
            this.stopAtrMultiplier = stopAtrMultiplier;
            this.pullbackToleranceAtr = pullbackToleranceAtr;
            this.pullbackCooldownBars = pullbackCooldownBars;
            this.currency = currency;
            this.maximumRiskPerContract = maximumRiskPerContract;
            this.riskLimitMode = riskLimitMode;
        }

        public string LastError { get; private set; }

        public bool Record(TrackedSignal trackedSignal)
        {
            try
            {
                string filePath = GetFilePath(trackedSignal.Signal.CreatedAt);
                string recordKey = BuildRecordKey(trackedSignal.Signal);
                string row = BuildRow(recordKey, trackedSignal);

                lock (FileLock)
                {
                    Directory.CreateDirectory(directory);
                    UpsertRow(filePath, recordKey, row);
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

        private string BuildRecordKey(TradeSignal signal)
        {
            return string.Join(
                "_",
                Sanitize(instrument),
                Sanitize(barsPeriod),
                signal.CreatedAt.ToString("yyyyMMddHHmmssfffffff", CultureInfo.InvariantCulture),
                signal.Setup.ToString(),
                signal.Direction.ToString(),
                Sanitize(version),
                fastEmaPeriod.ToString(CultureInfo.InvariantCulture),
                slowEmaPeriod.ToString(CultureInfo.InvariantCulture),
                atrPeriod.ToString(CultureInfo.InvariantCulture),
                stopAtrMultiplier.ToString("R", CultureInfo.InvariantCulture),
                pullbackToleranceAtr.ToString("R", CultureInfo.InvariantCulture),
                pullbackCooldownBars.ToString(CultureInfo.InvariantCulture),
                maximumRiskPerContract.ToString("R", CultureInfo.InvariantCulture),
                Sanitize(riskLimitMode),
                signal.RiskRewardRatio.ToString("R", CultureInfo.InvariantCulture),
                signal.ValidForBars.ToString(CultureInfo.InvariantCulture));
        }

        private string BuildRow(string recordKey, TrackedSignal trackedSignal)
        {
            TradeSignal signal = trackedSignal.Signal;
            ValidationProfile validationProfile = ValidationPlan.GetProfile(
                instrument,
                signal.Setup,
                signal.RiskRewardRatio);
            string closedAt = trackedSignal.ClosedAt.HasValue
                ? trackedSignal.ClosedAt.Value.ToString("O", CultureInfo.InvariantCulture)
                : string.Empty;

            return string.Join(
                ",",
                Csv(recordKey),
                Csv(signal.Id),
                Csv(signal.CreatedAt.ToString("O", CultureInfo.InvariantCulture)),
                Csv(closedAt),
                Csv(instrument),
                Csv(barsPeriod),
                Csv(version),
                Csv("Hypothetical"),
                Csv("SignalBarClose"),
                Csv("FollowingBarsHighLow"),
                Csv(ValidationPlan.RoundId),
                Csv(validationProfile.Stage.ToString()),
                Number(validationProfile.TargetR),
                Csv(ValidationPlan.GetSamplePhase(signal.CreatedAt)),
                Csv(signal.Setup.ToString()),
                Csv(signal.Direction.ToString()),
                Number(signal.EntryPrice),
                Number(signal.StopPrice),
                Number(signal.TargetPrice),
                Number(signal.TickSize),
                Number(signal.PointValue),
                Csv(currency),
                Number(signal.Risk),
                Number(signal.RiskTicks),
                Number(signal.RiskCurrency),
                Number(signal.RewardCurrency),
                Number(maximumRiskPerContract),
                Csv(riskLimitMode),
                Csv(GetRiskLimitStatus(signal)),
                Number(signal.RiskRewardRatio),
                signal.ValidForBars.ToString(CultureInfo.InvariantCulture),
                fastEmaPeriod.ToString(CultureInfo.InvariantCulture),
                slowEmaPeriod.ToString(CultureInfo.InvariantCulture),
                atrPeriod.ToString(CultureInfo.InvariantCulture),
                Number(stopAtrMultiplier),
                Number(pullbackToleranceAtr),
                pullbackCooldownBars.ToString(CultureInfo.InvariantCulture),
                Csv(trackedSignal.Status.ToString()),
                Number(trackedSignal.ResultR),
                Number(trackedSignal.MaximumFavorableExcursionR),
                Number(trackedSignal.MaximumAdverseExcursionR),
                trackedSignal.BarsElapsed.ToString(CultureInfo.InvariantCulture),
                Csv(trackedSignal.FirstEvent.ToString()),
                Csv(Iso(trackedSignal.FirstEventAt)),
                Number(signal.TargetOneRPrice),
                Csv(trackedSignal.TargetOneRStatus.ToString()),
                Csv(Iso(trackedSignal.TargetOneRAt)),
                Number(signal.TargetOnePointFiveRPrice),
                Csv(trackedSignal.TargetOnePointFiveRStatus.ToString()),
                Csv(Iso(trackedSignal.TargetOnePointFiveRAt)),
                Number(signal.TargetTwoRPrice),
                Csv(trackedSignal.TargetTwoRStatus.ToString()),
                Csv(Iso(trackedSignal.TargetTwoRAt)),
                Csv(signal.Reason));
        }

        private string GetFilePath(DateTime signalTime)
        {
            string fileName = string.Format(
                CultureInfo.InvariantCulture,
                "{0}_{1}_{2}_v6.csv",
                signalTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Sanitize(instrument),
                Sanitize(barsPeriod));
            return Path.Combine(directory, fileName);
        }

        private string GetRiskLimitStatus(TradeSignal signal)
        {
            if (maximumRiskPerContract <= 0)
                return "NotConfigured";

            return signal.RiskCurrency <= maximumRiskPerContract
                ? "WithinLimit"
                : "AboveLimit";
        }

        private static void UpsertRow(string filePath, string recordKey, string row)
        {
            List<string> lines = File.Exists(filePath)
                ? new List<string>(File.ReadAllLines(filePath, Encoding.UTF8))
                : new List<string>();
            string prefix = Csv(recordKey) + ",";
            int existingIndex = -1;

            for (int index = 1; index < lines.Count; index++)
            {
                if (lines[index].StartsWith(prefix, StringComparison.Ordinal))
                {
                    existingIndex = index;
                    break;
                }
            }

            if (lines.Count == 0)
                lines.Add(Header);

            if (existingIndex >= 0)
                lines[existingIndex] = row;
            else
                lines.Add(row);

            File.WriteAllLines(filePath, lines.ToArray(), new UTF8Encoding(true));
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

        private static string Iso(DateTime? value)
        {
            return value.HasValue
                ? value.Value.ToString("O", CultureInfo.InvariantCulture)
                : string.Empty;
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
