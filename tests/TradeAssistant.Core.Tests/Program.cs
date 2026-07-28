using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using NinjaTrader.NinjaScript.TradeAssistant.Analysis;
using NinjaTrader.NinjaScript.TradeAssistant.Configuration;
using NinjaTrader.NinjaScript.TradeAssistant.Models;
using NinjaTrader.NinjaScript.TradeAssistant.Persistence;

internal static class Program
{
    private static int Main()
    {
        try
        {
            ValidateProfiles();
            ValidateFrozenConfiguration();
            ValidateMarketContext();
            ValidateStatistics();
            ValidateCsvOutputs();
            Console.WriteLine("All Trade Assistant core tests passed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.ToString());
            return 1;
        }
    }

    private static void ValidateProfiles()
    {
        ValidationProfile mesBaseline = ValidationPlan.GetProfile("MES 09-26", SignalSetup.EmaCrossBaseline, 2);
        Assert(mesBaseline.Stage == ValidationStage.Reference && !mesBaseline.RenderOnChart, "MES baseline must be a hidden reference.");

        ValidationProfile mesPullback = ValidationPlan.GetProfile("MES 09-26", SignalSetup.TrendPullback, 2);
        Assert(mesPullback.Stage == ValidationStage.Reference && !mesPullback.RenderOnChart, "MES pullback must stay hidden.");

        ValidationProfile mesContext = ValidationPlan.GetProfile("MES 09-26", SignalSetup.ContextPullback, 2);
        Assert(mesContext.Stage == ValidationStage.Observation, "MES context must remain under observation.");
        Assert(mesContext.RenderOnChart && mesContext.TargetR == 1, "MES context must render at 1R.");

        ValidationProfile mnqPullback = ValidationPlan.GetProfile("MNQ 09-26", SignalSetup.TrendPullback, 2);
        Assert(mnqPullback.Stage == ValidationStage.Reference && !mnqPullback.RenderOnChart, "MNQ pullback must be a hidden reference.");

        ValidationProfile mnqContext = ValidationPlan.GetProfile("MNQ 09-26", SignalSetup.ContextPullback, 2);
        Assert(mnqContext.Stage == ValidationStage.Candidate, "MNQ context must be the candidate.");
        Assert(mnqContext.RenderOnChart && mnqContext.TargetR == 1, "MNQ context must render at 1R.");
        Assert(!ValidationPlan.IsForwardSample(new DateTime(2026, 7, 28)), "Discovery dates must remain historical reference.");
        Assert(ValidationPlan.IsForwardSample(new DateTime(2026, 7, 29)), "Context sample must start on July 29.");
    }

    private static void ValidateFrozenConfiguration()
    {
        Assert(
            ValidationPlan.MatchesFrozenConfiguration(9, 21, 14, 0.1, 3, 1.5, 2, 3, 50, RiskLimitMode.DescartarAcimaDoLimite),
            "Default validation settings must be accepted.");
        Assert(
            !ValidationPlan.MatchesFrozenConfiguration(10, 21, 14, 0.1, 3, 1.5, 2, 3, 50, RiskLimitMode.DescartarAcimaDoLimite),
            "Changed EMA must be rejected by the frozen round.");
    }

    private static void ValidateMarketContext()
    {
        MarketContextAnalyzer analyzer = new MarketContextAnalyzer();
        SignalContext approvedLong = analyzer.Analyze(
            SignalDirection.Long,
            101,
            100,
            99.9,
            0.3,
            0.1,
            102,
            98,
            0.4,
            0.8,
            1.1,
            1);
        Assert(approvedLong.Passed && approvedLong.Score == 6, "Aligned long context must pass.");

        SignalContext rejectedLong = analyzer.Analyze(
            SignalDirection.Long,
            99.5,
            100,
            100.1,
            0.3,
            0.1,
            102,
            98,
            0.1,
            0.5,
            0.5,
            1);
        Assert(!rejectedLong.Passed && rejectedLong.Score < MarketContextAnalyzer.MinimumScore, "Weak long context must fail.");

        SignalContext approvedShort = analyzer.Analyze(
            SignalDirection.Short,
            99,
            100,
            100.1,
            -0.3,
            -0.1,
            102,
            98,
            0.4,
            0.2,
            1.1,
            1);
        Assert(approvedShort.Passed && approvedShort.Score == 6, "Aligned short context must pass.");
    }

    private static void ValidateStatistics()
    {
        DateTime day = new DateTime(2026, 7, 22, 9, 0, 0);
        List<TrackedSignal> signals = new List<TrackedSignal>
        {
            Tracked("winner", day, 10, ComparisonStatus.TargetHit),
            Tracked("loss-1", day.AddMinutes(5), 20, ComparisonStatus.StopHit),
            Tracked("loss-2", day.AddMinutes(10), 30, ComparisonStatus.StopHit),
            Tracked("expired", day.AddMinutes(15), 40, ComparisonStatus.Expired)
        };

        ValidationStatistics statistics = ValidationStatisticsCalculator.Calculate(
            signals,
            SignalSetup.EmaCrossBaseline,
            1,
            day);

        Assert(statistics.Decided == 3 && statistics.TargetHits == 1 && statistics.StopHits == 2, "Decided results are incorrect.");
        Assert(statistics.ResultR == -1, "Result in R is incorrect.");
        Assert(statistics.ResultCurrency == -40, "Currency result is incorrect.");
        Assert(statistics.MaximumConsecutiveLosses == 2, "Losing streak is incorrect.");
        Assert(statistics.MaximumDrawdownR == 2, "R drawdown is incorrect.");
        Assert(statistics.MaximumDrawdownCurrency == 50, "Currency drawdown is incorrect.");
        Assert(statistics.AverageWinnerRiskCurrency == 10, "Average winner risk is incorrect.");
        Assert(statistics.AverageLoserRiskCurrency == 25, "Average loser risk is incorrect.");
    }

    private static void ValidateCsvOutputs()
    {
        string directory = Path.Combine(Path.GetTempPath(), "trade-assistant-core-tests-" + Guid.NewGuid().ToString("N"));
        string dataDirectory = Path.Combine(directory, "Data");
        string summaryDirectory = Path.Combine(directory, "Summaries");
        DateTime day = new DateTime(2026, 7, 22, 9, 0, 0);
        TrackedSignal signal = Tracked("csv", day, 25, ComparisonStatus.TargetHit);

        try
        {
            CsvSignalJournal journal = new CsvSignalJournal(
                dataDirectory, "MES 09-26", "Minute-5", TradeAssistantVersion.Current,
                9, 21, 14, 1.5, 0.1, 3, "USD", 50, "DescartarAcimaDoLimite");
            Assert(journal.Record(signal), "Raw CSV could not be written: " + journal.LastError);

            string[] rawFiles = Directory.GetFiles(dataDirectory, "*_v8.csv");
            Assert(rawFiles.Length == 1, "CSV v8 was not created.");
            string raw = File.ReadAllText(rawFiles[0]);
            Assert(raw.Contains("ValidationRound,ValidationStage,ValidationTargetR,ValidationSample"), "CSV v8 validation columns are missing.");
            Assert(raw.Contains("SessionVwap,VwapSlopeAtr,VwapDistanceAtr"), "CSV v8 context columns are missing.");
            Assert(raw.Contains(ValidationPlan.RoundId + ",Reference,1,HistoricalReference"), "CSV v8 profile was not recorded.");

            TrackedSignal staleSignal = Tracked("stale", day.AddMinutes(5), 30, ComparisonStatus.StopHit);
            Assert(journal.Record(staleSignal), "Stale test row could not be written: " + journal.LastError);
            Assert(File.ReadAllLines(rawFiles[0]).Length == 3, "Stale test row was not added.");
            Assert(
                journal.SynchronizeDay(new List<TrackedSignal> { signal }, day),
                "Daily snapshot could not be synchronized: " + journal.LastError);
            Assert(File.ReadAllLines(rawFiles[0]).Length == 2, "Daily synchronization did not remove the stale row.");

            CsvValidationSummaryJournal summary = new CsvValidationSummaryJournal(
                summaryDirectory, "MES 09-26", "Minute-5", TradeAssistantVersion.Current, "USD", 2);
            Assert(summary.Record(new List<TrackedSignal> { signal }), "Summary CSV could not be written: " + summary.LastError);

            string[] summaryFiles = Directory.GetFiles(summaryDirectory, "*_validation_v3.csv");
            Assert(summaryFiles.Length == 1, "Daily summary was not created.");
            string summaryText = File.ReadAllText(summaryFiles[0]);
            Assert(summaryText.Contains("MaximumConsecutiveLosses,MaximumDrawdownR,MaximumDrawdownCurrency"), "Summary risk metrics are missing.");
            Assert(summaryText.Contains("HistoricalReference,No,EmaCrossBaseline"), "Historical summary must be ineligible.");
            Assert(summaryText.Contains("EmaCrossBaseline,Reference,1"), "Reference summary row is missing.");

            string analysisDirectory = Path.Combine(directory, "Analysis");
            CsvValidationSegmentJournal segments = new CsvValidationSegmentJournal(
                analysisDirectory, "MES 09-26", "Minute-5", TradeAssistantVersion.Current, "USD", 2);
            Assert(segments.Record(new List<TrackedSignal> { signal }), "Segment CSV could not be written: " + segments.LastError);
            string[] segmentFiles = Directory.GetFiles(analysisDirectory, "*_segments_v2.csv");
            Assert(segmentFiles.Length == 1, "Segment CSV was not created.");
            string segmentText = File.ReadAllText(segmentFiles[0]);
            Assert(segmentText.Contains("Direction,Long"), "Direction segment is missing.");
            Assert(segmentText.Contains("Hour,09:00"), "Hour segment is missing.");
            Assert(segmentText.Contains("RiskBand,0-25"), "Risk band segment is missing.");
            Assert(segmentText.Contains("ContextScore,0"), "Context score segment is missing.");
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
    }

    private static TrackedSignal Tracked(string id, DateTime time, double riskCurrency, ComparisonStatus status)
    {
        double riskPoints = riskCurrency / 5.0;
        TradeSignal signal = new TradeSignal(
            id,
            SignalSetup.EmaCrossBaseline,
            SignalDirection.Long,
            100,
            100 - riskPoints,
            100 + (riskPoints * 2),
            0.25,
            5,
            time,
            3,
            1,
            "test");
        TrackedSignal tracked = new TrackedSignal(signal, 0);
        tracked.TargetOneRStatus = status;
        tracked.TargetOnePointFiveRStatus = status;
        tracked.TargetTwoRStatus = status;
        tracked.Status = status == ComparisonStatus.TargetHit
            ? SignalStatus.TargetHit
            : status == ComparisonStatus.StopHit
                ? SignalStatus.StopHit
                : SignalStatus.Expired;
        return tracked;
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
