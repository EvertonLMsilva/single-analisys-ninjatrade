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
        Assert(mesBaseline.Stage == ValidationStage.Candidate, "MES baseline must be the candidate.");
        Assert(mesBaseline.RenderOnChart && mesBaseline.TargetR == 1, "MES baseline must render at 1R.");

        ValidationProfile mesPullback = ValidationPlan.GetProfile("MES 09-26", SignalSetup.TrendPullback, 2);
        Assert(mesPullback.Stage == ValidationStage.Paused && !mesPullback.RenderOnChart, "MES pullback must stay hidden.");

        ValidationProfile mnqPullback = ValidationPlan.GetProfile("MNQ 09-26", SignalSetup.TrendPullback, 2);
        Assert(mnqPullback.Stage == ValidationStage.Observation, "MNQ pullback must remain under observation.");
        Assert(mnqPullback.RenderOnChart && mnqPullback.TargetR == 1.5, "MNQ pullback must render at 1.5R.");
    }

    private static void ValidateFrozenConfiguration()
    {
        Assert(
            ValidationPlan.MatchesFrozenConfiguration(9, 21, 14, 0.1, 3, 1.5, 2, 3, 75, RiskLimitMode.DescartarAcimaDoLimite),
            "Default validation settings must be accepted.");
        Assert(
            !ValidationPlan.MatchesFrozenConfiguration(10, 21, 14, 0.1, 3, 1.5, 2, 3, 75, RiskLimitMode.DescartarAcimaDoLimite),
            "Changed EMA must be rejected by the frozen round.");
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
                dataDirectory, "MES 09-26", "Minute-5", "0.8.0-beta.1",
                9, 21, 14, 1.5, 0.1, 3, "USD", 75, "DescartarAcimaDoLimite");
            Assert(journal.Record(signal), "Raw CSV could not be written: " + journal.LastError);

            string[] rawFiles = Directory.GetFiles(dataDirectory, "*_v6.csv");
            Assert(rawFiles.Length == 1, "CSV v6 was not created.");
            string raw = File.ReadAllText(rawFiles[0]);
            Assert(raw.Contains("ValidationRound,ValidationStage,ValidationTargetR"), "CSV v6 validation columns are missing.");
            Assert(raw.Contains(ValidationPlan.RoundId + ",Candidate,1"), "CSV v6 profile was not recorded.");

            CsvValidationSummaryJournal summary = new CsvValidationSummaryJournal(
                summaryDirectory, "MES 09-26", "Minute-5", "0.8.0-beta.1", "USD", 2);
            Assert(summary.Record(new List<TrackedSignal> { signal }), "Summary CSV could not be written: " + summary.LastError);

            string[] summaryFiles = Directory.GetFiles(summaryDirectory, "*_validation_v1.csv");
            Assert(summaryFiles.Length == 1, "Daily summary was not created.");
            string summaryText = File.ReadAllText(summaryFiles[0]);
            Assert(summaryText.Contains("MaximumConsecutiveLosses,MaximumDrawdownR,MaximumDrawdownCurrency"), "Summary risk metrics are missing.");
            Assert(summaryText.Contains("EmaCrossBaseline,Candidate,1"), "Candidate summary row is missing.");
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
