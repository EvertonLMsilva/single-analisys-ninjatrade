using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using NinjaTrader.NinjaScript.TradeAssistant.Analysis;
using NinjaTrader.NinjaScript.TradeAssistant.Configuration;
using NinjaTrader.NinjaScript.TradeAssistant.Models;
using NinjaTrader.NinjaScript.TradeAssistant.Persistence;
using NinjaTrader.NinjaScript.TradeAssistant.Tracking;

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
            ValidateIntradayMomentum();
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
        Assert(mesContext.Stage == ValidationStage.Reference, "MES context must remain a reference.");
        Assert(!mesContext.RenderOnChart && mesContext.TargetR == 1, "MES context must stay hidden at 1R.");

        ValidationProfile mnqPullback = ValidationPlan.GetProfile("MNQ 09-26", SignalSetup.TrendPullback, 2);
        Assert(mnqPullback.Stage == ValidationStage.Reference && !mnqPullback.RenderOnChart, "MNQ pullback must be a hidden reference.");

        ValidationProfile mnqContext = ValidationPlan.GetProfile("MNQ 09-26", SignalSetup.ContextPullback, 2);
        Assert(mnqContext.Stage == ValidationStage.Reference, "Strict MNQ context must become a hidden reference.");
        Assert(!mnqContext.RenderOnChart && mnqContext.TargetR == 1, "Strict MNQ context must stay hidden at 1R.");

        ValidationProfile mnqEvidence = ValidationPlan.GetProfile("MNQ 09-26", SignalSetup.EvidencePullback, 2);
        Assert(mnqEvidence.Stage == ValidationStage.Reference, "Old MNQ evidence pullback must become a reference.");
        Assert(!mnqEvidence.RenderOnChart && mnqEvidence.TargetR == 1, "Old MNQ evidence pullback must stay hidden at 1R.");

        ValidationProfile mnqQualified = ValidationPlan.GetProfile("MNQ 09-26", SignalSetup.QualifiedPullback, 2);
        Assert(mnqQualified.Stage == ValidationStage.Candidate, "Qualified MNQ pullback must be the candidate.");
        Assert(
            mnqQualified.RenderOnChart && mnqQualified.TargetR == ValidationPlan.QualifiedTargetR,
            "Qualified MNQ pullback must render at the frozen 1.5R target.");

        ValidationProfile mesQualified = ValidationPlan.GetProfile("MES 09-26", SignalSetup.QualifiedPullback, 2);
        Assert(
            mesQualified.Stage == ValidationStage.Reference
                && !mesQualified.RenderOnChart
                && mesQualified.TargetR == ValidationPlan.QualifiedTargetR,
            "MES qualified profile must stay hidden.");
        Assert(!ValidationPlan.IsForwardSample(new DateTime(2026, 7, 29)), "Selection dates must remain historical reference.");
        Assert(ValidationPlan.IsForwardSample(new DateTime(2026, 7, 30)), "Qualified sample must start on July 30.");
    }

    private static void ValidateFrozenConfiguration()
    {
        Assert(
            ValidationPlan.MatchesFrozenConfiguration(9, 21, 14, 0.1, 3, 1.5, 2, 3, 50, RiskLimitMode.DescartarAcimaDoLimite),
            "Default validation settings must be accepted.");
        Assert(
            !ValidationPlan.MatchesFrozenConfiguration(10, 21, 14, 0.1, 3, 1.5, 2, 3, 50, RiskLimitMode.DescartarAcimaDoLimite),
            "Changed EMA must be rejected by the frozen round.");
        Assert(
            ValidationPlan.NormalizeMaximumRiskPerContract(75, RiskLimitMode.DescartarAcimaDoLimite) == 50,
            "A saved legacy risk limit must be capped at the frozen validation limit.");
        Assert(
            ValidationPlan.NormalizeMaximumRiskPerContract(40, RiskLimitMode.DescartarAcimaDoLimite) == 40,
            "A stricter configured risk limit must not be relaxed.");
        Assert(
            ValidationPlan.NormalizeMaximumRiskPerContract(75, RiskLimitMode.SomenteAvisar) == 75,
            "Warning-only configurations must not be silently changed.");
        Assert(ValidationPlan.IsQualifiedRiskEligible(5), "Qualified minimum risk must be accepted.");
        Assert(ValidationPlan.IsQualifiedRiskEligible(50), "Qualified maximum risk must be accepted.");
        Assert(!ValidationPlan.IsQualifiedRiskEligible(4.5), "Risk below USD 5 must be rejected.");
        Assert(!ValidationPlan.IsQualifiedRiskEligible(50.5), "Risk above USD 50 must be rejected.");
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
        Assert(
            ValidationPlan.MatchesEvidenceRule(
                "MNQ 09-26",
                SignalDirection.Short,
                99,
                approvedShort),
            "MNQ short below a falling VWAP with score 4+ must pass the evidence rule.");
        Assert(
            !ValidationPlan.MatchesEvidenceRule(
                "MNQ 09-26",
                SignalDirection.Long,
                101,
                approvedLong),
            "Long signals must not pass the selected evidence rule.");
        Assert(
            !ValidationPlan.MatchesEvidenceRule(
                "MES 09-26",
                SignalDirection.Short,
                99,
                approvedShort),
            "MES must remain outside the selected evidence rule.");
        Assert(
            ValidationPlan.MatchesQualifiedRule(
                "MNQ 09-26",
                SignalDirection.Short,
                approvedShort),
            "Aligned MNQ short with score 5+, distance up to 2 ATR and volume 1+ must qualify.");
        Assert(
            !ValidationPlan.MatchesQualifiedRule(
                "MNQ 09-26",
                SignalDirection.Long,
                approvedLong),
            "Long signals must not pass the qualified rule.");
        Assert(
            !ValidationPlan.MatchesQualifiedRule(
                "MES 09-26",
                SignalDirection.Short,
                approvedShort),
            "MES must not pass the qualified rule.");
        SignalContext lowVolume = new SignalContext(
            100, -0.1, 1, -0.2, -0.1, 102, 98, 0.3, 0.2, 0.9, 5, true, "low volume");
        Assert(
            !ValidationPlan.MatchesQualifiedRule(
                "MNQ 09-26",
                SignalDirection.Short,
                lowVolume),
            "Relative volume below 1 must reject the qualified rule.");
        SignalContext tooFarFromVwap = new SignalContext(
            100, -0.1, 2.01, -0.2, -0.1, 103, 97, 0.3, 0.2, 1.1, 5, true, "too far");
        Assert(
            !ValidationPlan.MatchesQualifiedRule(
                "MNQ 09-26",
                SignalDirection.Short,
                tooFarFromVwap),
            "VWAP distance above 2 ATR must reject the qualified rule.");
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

    private static void ValidateIntradayMomentum()
    {
        Assert(
            IntradayMomentumPlan.IsEligibleInstrument("MNQ 09-26"),
            "MNQ must be eligible for intraday momentum.");
        Assert(
            !IntradayMomentumPlan.IsEligibleInstrument("MES 09-26"),
            "MES must remain outside this frozen candidate.");
        Assert(
            IntradayMomentumPlan.GetRegularOpen(new DateTime(2026, 7, 30)).Hour == 10,
            "US daylight-saving session must open at 10:30 BRT.");
        Assert(
            IntradayMomentumPlan.GetRegularOpen(new DateTime(2026, 12, 1)).Hour == 11,
            "US standard-time session must open at 11:30 BRT.");

        IntradayMomentumTracker tracker =
            new IntradayMomentumTracker(0.25, 2.0);
        DateTime start = new DateTime(2026, 6, 1);
        for (int dayIndex = 0; dayIndex < 21; dayIndex++)
        {
            FeedMomentumDay(
                tracker,
                start.AddDays(dayIndex),
                1.0,
                100 + (dayIndex % 3),
                100,
                100,
                false);
        }

        List<IntradayMomentumUpdate> stopDay = FeedMomentumDay(
            tracker,
            start.AddDays(21),
            2.0,
            110,
            108,
            110,
            true);
        IntradayMomentumTrade opened = FindOpened(stopDay);
        IntradayMomentumTrade stopped = FindClosed(stopDay);
        Assert(opened != null, "The first session after warmup must open a candidate.");
        Assert(opened.Direction == SignalDirection.Long, "High-volatility direction must follow the opening return.");
        Assert(opened.Regime == IntradayMomentumRegime.HighOpeningVolatility, "Opening volatility regime is incorrect.");
        Assert(stopped != null && stopped.Status == IntradayMomentumStatus.StopHit, "The fixed hypothetical stop must be tracked.");
        Assert(stopped.ResultCurrency == -80, "MNQ stop result must include the USD 5 round-turn cost.");

        List<IntradayMomentumUpdate> closeDay = FeedMomentumDay(
            tracker,
            start.AddDays(22),
            0.5,
            102,
            101,
            103,
            false,
            110);
        IntradayMomentumTrade timeExit = FindClosed(closeDay);
        Assert(timeExit != null && timeExit.Status == IntradayMomentumStatus.TimeExit, "An active candidate must exit at the regular close.");
        Assert(timeExit.ResultCurrency == 9, "Time-exit result must use MNQ point value and include costs.");

        string directory = Path.Combine(
            Path.GetTempPath(),
            "trade-assistant-momentum-tests-" + Guid.NewGuid().ToString("N"));
        try
        {
            CsvIntradayMomentumJournal journal =
                new CsvIntradayMomentumJournal(
                    directory,
                    "MNQ 09-26",
                    "Minute-1",
                    TradeAssistantVersion.Current);
            Assert(journal.Record(timeExit), "Momentum CSV could not be written: " + journal.LastError);
            string[] files = Directory.GetFiles(directory, "*_intraday_momentum_v1.csv");
            Assert(files.Length == 1, "Momentum CSV was not created.");
            string text = File.ReadAllText(files[0]);
            Assert(text.Contains("RoundTurnCost,Status,ResultCurrency,ResultR"), "Momentum result columns are missing.");
            Assert(text.Contains(IntradayMomentumPlan.RoundId), "Frozen momentum round was not recorded.");
            Assert(File.ReadAllLines(files[0]).Length == 2, "Momentum journal must keep one row per session.");
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
    }

    private static List<IntradayMomentumUpdate> FeedMomentumDay(
        IntradayMomentumTracker tracker,
        DateTime day,
        double openingRange,
        double firstHalfClose,
        double penultimateStart,
        double entryPrice,
        bool hitStop,
        double regularClosePrice = 100)
    {
        List<IntradayMomentumUpdate> updates =
            new List<IntradayMomentumUpdate>();
        DateTime regularOpen = IntradayMomentumPlan.GetRegularOpen(day);
        for (int minute = 1; minute <= 390; minute++)
        {
            double open = 100;
            double close = 100;
            double range = minute <= 30 ? openingRange : 0.5;
            if (minute == 30)
                close = firstHalfClose;
            if (minute == 330)
                open = penultimateStart;
            if (minute == 360)
                open = entryPrice;
            if (minute == 390)
                close = regularClosePrice;

            double high = Math.Max(open, close) + (range / 2.0);
            double low = Math.Min(open, close) - (range / 2.0);
            if (hitStop && minute == 361)
                low = entryPrice - 38;

            IntradayMomentumUpdate update = tracker.Update(
                regularOpen.AddMinutes(minute),
                open,
                high,
                low,
                close,
                1000);
            if (update.HasChanges)
                updates.Add(update);
        }
        return updates;
    }

    private static IntradayMomentumTrade FindOpened(
        IList<IntradayMomentumUpdate> updates)
    {
        foreach (IntradayMomentumUpdate update in updates)
        {
            if (update.OpenedTrade != null)
                return update.OpenedTrade;
        }
        return null;
    }

    private static IntradayMomentumTrade FindClosed(
        IList<IntradayMomentumUpdate> updates)
    {
        foreach (IntradayMomentumUpdate update in updates)
        {
            if (update.ClosedTrade != null)
                return update.ClosedTrade;
        }
        return null;
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
