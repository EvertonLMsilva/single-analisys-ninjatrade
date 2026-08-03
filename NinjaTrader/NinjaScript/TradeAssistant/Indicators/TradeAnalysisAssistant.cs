using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.TradeAssistant.Analysis;
using NinjaTrader.NinjaScript.TradeAssistant.Configuration;
using NinjaTrader.NinjaScript.TradeAssistant.MarketData;
using NinjaTrader.NinjaScript.TradeAssistant.Models;
using NinjaTrader.NinjaScript.TradeAssistant.Persistence;
using NinjaTrader.NinjaScript.TradeAssistant.Tracking;

namespace NinjaTrader.NinjaScript.Indicators
{
    public class TradeAnalysisAssistant : Indicator
    {
        private ATR atr;
        private double cumulativeSessionPriceVolume;
        private double cumulativeSessionVolume;
        private EMA fastEma;
        private string instrumentCurrency;
        private double instrumentPointValue;
        private double instrumentTickSize;
        private int lastLongPullbackBar;
        private int lastShortPullbackBar;
        private MarketContextAnalyzer marketContextAnalyzer;
        private MarketDeltaTracker marketDeltaTracker;
        private MarketDeltaSnapshot latestDeltaSnapshot;
        private double previousSessionVwap;
        private double sessionHigh;
        private double sessionLow;
        private double sessionVwap;
        private EMA slowEma;
        private SMA volumeAverage;
        private HashSet<string> visibleSignalIds;
        private Queue<string> visualSignalIds;
        private SignalAnalyzer signalAnalyzer;
        private CsvSignalJournal signalJournal;
        private CsvIntradayMomentumJournal intradayMomentumJournal;
        private CsvValidationSegmentJournal validationSegmentJournal;
        private CsvValidationSummaryJournal validationSummaryJournal;
        private IntradayMomentumTracker intradayMomentumTracker;
        private SignalTracker signalTracker;
        private bool validationConfigurationMatches;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Indicador visual de análise hipotética sem execução automática de ordens. Versão "
                    + TradeAssistantVersion.Current
                    + ".";

                Name = "Trade Analysis Assistant";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                IsChartOnly = true;
                DisplayInDataBox = false;
                DrawOnPricePanel = true;

                EnableLongSignals = true;
                EnableShortSignals = true;
                
                EnableContextPullbackSignal = false;
                EnableContextMomentumSignal = true;

                EnableBaselineComparison = false;

                FastEmaPeriod = 9;
                SlowEmaPeriod = 21;
                AtrPeriod = 14;

                PullbackToleranceAtr = 0.1;
                PullbackCooldownBars = 3;

                StopAtrMultiplier = 1.5;
                RiskRewardRatio = 2.0;
                ValidForBars = 3;

                ShowHistoricalSignals = true;
                MaxHistoricalSignals = 20;
                ZoneOpacity = 14;

                EnableCsvJournal = true;
                EnableValidationMode = true;
                EnableUniversalRealtimeAnalysis = true;
                EnableIntradayMomentumCandidate = false;

                RealtimeStartTime =
                    UniversalRealtimePlan.DefaultStartTime;

                RealtimeEndTime =
                    UniversalRealtimePlan.DefaultEndTime;

                MaximumRiskPerContract = 50;
                RiskLimitPolicy =
                    RiskLimitMode.DescartarAcimaDoLimite;
            }
            else if (State == State.Configure)
            {
                if (!EnableUniversalRealtimeAnalysis
                    && EnableIntradayMomentumCandidate)
                {
                    AddDataSeries(
                        BarsPeriodType.Minute,
                        1);
                }
            }
            else if (State == State.DataLoaded)
            {
                fastEma = EMA(FastEmaPeriod);
                slowEma = EMA(SlowEmaPeriod);
                atr = ATR(AtrPeriod);

                volumeAverage = SMA(
                    Volume,
                    ValidationPlan.VolumeAveragePeriod);

                instrumentCurrency = GetCurrencyCode(
                    Bars.Instrument.MasterInstrument.Currency
                        .ToString());

                instrumentPointValue =
                    Bars.Instrument.MasterInstrument.PointValue;

                instrumentTickSize =
                    Bars.Instrument.MasterInstrument.TickSize;

                marketDeltaTracker =
                    new MarketDeltaTracker(
                        UniversalRealtimePlan
                            .RequiredMinutePeriod,
                        instrumentTickSize);

                latestDeltaSnapshot = null;

                signalAnalyzer =
                    new SignalAnalyzer();

                marketContextAnalyzer =
                    new MarketContextAnalyzer();

                signalTracker =
                    new SignalTracker();

                if (!EnableUniversalRealtimeAnalysis
                    && EnableIntradayMomentumCandidate
                    && IntradayMomentumPlan
                        .IsEligibleInstrument(
                            Bars.Instrument.FullName))
                {
                    intradayMomentumTracker =
                        new IntradayMomentumTracker(
                            instrumentTickSize,
                            instrumentPointValue);
                }

                if (EnableValidationMode)
                {
                    MaximumRiskPerContract =
                        ValidationPlan
                            .NormalizeMaximumRiskPerContract(
                                MaximumRiskPerContract,
                                RiskLimitPolicy);
                }

                validationConfigurationMatches =
                    ValidationPlan
                        .MatchesFrozenConfiguration(
                            FastEmaPeriod,
                            SlowEmaPeriod,
                            AtrPeriod,
                            PullbackToleranceAtr,
                            PullbackCooldownBars,
                            StopAtrMultiplier,
                            RiskRewardRatio,
                            ValidForBars,
                            MaximumRiskPerContract,
                            RiskLimitPolicy);

                lastLongPullbackBar = -1000000;
                lastShortPullbackBar = -1000000;

                if (EnableCsvJournal)
                {
                    string journalDirectory =
                        System.IO.Path.Combine(
                            NinjaTrader.Core.Globals
                                .UserDataDir,
                            "TradeAssistant",
                            EnableUniversalRealtimeAnalysis
                                ? "Realtime"
                                : "Data");

                    string barsPeriodDescription =
                        BarsPeriod.BarsPeriodType
                        + "-"
                        + BarsPeriod.Value;

                    signalJournal =
                        new CsvSignalJournal(
                            journalDirectory,
                            Bars.Instrument.FullName,
                            barsPeriodDescription,
                            TradeAssistantVersion.Current,
                            FastEmaPeriod,
                            SlowEmaPeriod,
                            AtrPeriod,
                            StopAtrMultiplier,
                            PullbackToleranceAtr,
                            PullbackCooldownBars,
                            instrumentCurrency,
                            MaximumRiskPerContract,
                            RiskLimitPolicy.ToString(),
                            EnableUniversalRealtimeAnalysis);

                    if (!EnableUniversalRealtimeAnalysis)
                    {
                        validationSummaryJournal =
                            new CsvValidationSummaryJournal(
                                System.IO.Path.Combine(
                                    NinjaTrader.Core.Globals
                                        .UserDataDir,
                                    "TradeAssistant",
                                    "Summaries"),
                                Bars.Instrument.FullName,
                                barsPeriodDescription,
                                TradeAssistantVersion.Current,
                                instrumentCurrency,
                                RiskRewardRatio);

                        validationSegmentJournal =
                            new CsvValidationSegmentJournal(
                                System.IO.Path.Combine(
                                    NinjaTrader.Core.Globals
                                        .UserDataDir,
                                    "TradeAssistant",
                                    "Analysis"),
                                Bars.Instrument.FullName,
                                barsPeriodDescription,
                                TradeAssistantVersion.Current,
                                instrumentCurrency,
                                RiskRewardRatio);
                    }

                    if (intradayMomentumTracker != null)
                    {
                        intradayMomentumJournal =
                            new CsvIntradayMomentumJournal(
                                System.IO.Path.Combine(
                                    NinjaTrader.Core.Globals
                                        .UserDataDir,
                                    "TradeAssistant",
                                    "IntradayMomentum"),
                                Bars.Instrument.FullName,
                                "Minute-1",
                                TradeAssistantVersion.Current);
                    }
                }

                visibleSignalIds =
                    new HashSet<string>();

                visualSignalIds =
                    new Queue<string>();
            }
            else if (State == State.Terminated)
            {
                if (marketDeltaTracker != null)
                {
                    marketDeltaTracker.Reset();
                    marketDeltaTracker = null;
                }

                latestDeltaSnapshot = null;
            }
        }

        protected override void OnMarketData(
            MarketDataEventArgs marketDataUpdate)
        {
            if (!EnableUniversalRealtimeAnalysis)
                return;

            if (marketDeltaTracker == null
                || marketDataUpdate == null)
            {
                return;
            }

            if (marketDataUpdate.MarketDataType
                != MarketDataType.Last)
            {
                return;
            }

            if (marketDataUpdate.Volume <= 0)
                return;

            marketDeltaTracker.ProcessTrade(
                marketDataUpdate.Time,
                marketDataUpdate.Price,
                marketDataUpdate.Volume,
                marketDataUpdate.Bid,
                marketDataUpdate.Ask);
        }

        protected override void OnBarUpdate()
        {
            if (BarsInProgress > 0)
            {
                EvaluateIntradayMomentum();
                return;
            }

            if (BarsInProgress != 0)
                return;

            UpdateSessionContext();

            latestDeltaSnapshot =
                EnableUniversalRealtimeAnalysis
                && marketDeltaTracker != null
                    ? marketDeltaTracker.GetSnapshot(
                        Time[0])
                    : null;

            int requiredBars = Math.Max(
                Math.Max(
                    SlowEmaPeriod,
                    AtrPeriod),
                ValidationPlan.VolumeAveragePeriod)
                + ValidationPlan.EmaSlopeLookbackBars;

            if (CurrentBar < requiredBars)
                return;

            foreach (
                TrackedSignal closedSignal
                in signalTracker.Update(
                    High[0],
                    Low[0],
                    Time[0],
                    CurrentBar))
            {
                RecordSignal(closedSignal);
                RenderOutcome(closedSignal);
            }

            if (EnableUniversalRealtimeAnalysis)
            {
                if (IsUniversalRealtimeTimeframe()
                    && IsInsideRealtimeWindow(
                        Time[0]))
                {
                    EvaluatePullbackSetup();
                }
            }
            else if (intradayMomentumTracker == null
                && (!EnableValidationMode
                    || validationConfigurationMatches))
            {
                EvaluatePullbackSetup();
                EvaluateBaselineSetup();
            }

            RenderModePanel();
        }

        private void EvaluateIntradayMomentum()
        {
            if (intradayMomentumTracker == null
                || CurrentBars[1] < 1)
            {
                return;
            }

            IntradayMomentumUpdate update =
                intradayMomentumTracker.Update(
                    Times[1][0],
                    Opens[1][0],
                    Highs[1][0],
                    Lows[1][0],
                    Closes[1][0],
                    Volumes[1][0]);

            if (!update.HasChanges)
                return;

            if (update.OpenedTrade != null)
            {
                RecordIntradayMomentum(
                    update.OpenedTrade);

                RenderIntradayMomentum(
                    update.OpenedTrade);
            }

            if (update.ClosedTrade != null)
            {
                RecordIntradayMomentum(
                    update.ClosedTrade);

                RenderIntradayMomentumOutcome(
                    update.ClosedTrade);
            }
        }

        private void RecordIntradayMomentum(
            IntradayMomentumTrade trade)
        {
            if (intradayMomentumJournal != null
                && !intradayMomentumJournal
                    .Record(trade))
            {
                Print(
                    "Trade Assistant: nao foi possivel "
                    + "gravar o momentum intradiario. "
                    + intradayMomentumJournal.LastError);
            }
        }

        private void EvaluatePullbackSetup()
        {
            bool baselineActive =
                signalTracker.HasActiveSignal(
                    SignalSetup.TrendPullback);

            bool contextActive =
                signalTracker.HasActiveSignal(
                    SignalSetup.ContextPullback);

            bool evidenceActive =
                signalTracker.HasActiveSignal(
                    SignalSetup.EvidencePullback);

            bool qualifiedActive =
                signalTracker.HasActiveSignal(
                    SignalSetup.QualifiedPullback);

            if (!EnablePullbackSignals)
                return;

            /*
            * No modo universal utilizamos ContextPullback
            * como o tipo comum dos sinais visuais.
            *
            * Enquanto houver um sinal ativo, não será criado
            * outro sinal simultâneo.
            */
            if (EnableUniversalRealtimeAnalysis
                && contextActive)
            {
                return;
            }

            if (!EnableUniversalRealtimeAnalysis
                && baselineActive
                && contextActive
                && evidenceActive
                && qualifiedActive)
            {
                return;
            }

            double normalizedAtr =
                atr[0] > 0
                    ? atr[0]
                    : instrumentTickSize;

            double tolerance =
                normalizedAtr
                * PullbackToleranceAtr;

            double candleRange =
                High[0] - Low[0];

            double candleBody =
                Math.Abs(
                    Close[0] - Open[0]);

            double candleBodyAtr =
                candleBody / normalizedAtr;

            double closeLocation =
                candleRange > 0
                    ? (Close[0] - Low[0])
                        / candleRange
                    : 0.5;

            double averageVolume =
                volumeAverage[0];

            double relativeVolume =
                averageVolume > 0
                    ? Volume[0] / averageVolume
                    : 0;

            double distanceFromFastEmaAtr =
                Math.Abs(
                    Close[0] - fastEma[0])
                / normalizedAtr;

            bool longTrend =
                fastEma[0] > slowEma[0]
                && fastEma[0] > fastEma[1]
                && slowEma[0] >= slowEma[1];

            bool shortTrend =
                fastEma[0] < slowEma[0]
                && fastEma[0] < fastEma[1]
                && slowEma[0] <= slowEma[1];

            /*
            * =====================================================
            * PULLBACK CONTEXTUAL
            * =====================================================
            *
            * Agora é opcional e mais seletivo:
            *
            * - tendência confirmada;
            * - retorno à EMA rápida;
            * - candle de retomada;
            * - fechamento em região favorável;
            * - volume mínimo;
            * - contexto completo;
            * - risco dentro do limite.
            */

            if (EnableContextPullbackSignal)
            {
                bool longPullbackConfirmation =
                    longTrend
                    && Low[0] <= fastEma[0] + tolerance
                    && Close[0] > fastEma[0]
                    && Close[0] > Open[0]
                    && closeLocation >= 0.60
                    && candleBodyAtr >= 0.20
                    && relativeVolume >= 0.90;

                if (EnableLongSignals
                    && longPullbackConfirmation
                    && PassesRealtimeDeltaFilter(
                        SignalDirection.Long)
                    && CurrentBar - lastLongPullbackBar
                        >= PullbackCooldownBars)
                {
                    bool longSignalCreated =
                        RegisterPullbackCandidates(
                            SignalDirection.Long,
                            Low[0] - instrumentTickSize,
                            baselineActive,
                            contextActive,
                            evidenceActive,
                            qualifiedActive,
                            "PULLBACK CONTEXTUAL",
                            true);

                    if (longSignalCreated)
                    {
                        lastLongPullbackBar =
                            CurrentBar;

                        return;
                    }
                }

                bool shortPullbackConfirmation =
                    shortTrend
                    && High[0] >= fastEma[0] - tolerance
                    && Close[0] < fastEma[0]
                    && Close[0] < Open[0]
                    && closeLocation <= 0.40
                    && candleBodyAtr >= 0.20
                    && relativeVolume >= 0.90;

                if (EnableShortSignals
                    && shortPullbackConfirmation
                    && PassesRealtimeDeltaFilter(
                        SignalDirection.Short)
                    && CurrentBar - lastShortPullbackBar
                        >= PullbackCooldownBars)
                {
                    bool shortSignalCreated =
                        RegisterPullbackCandidates(
                            SignalDirection.Short,
                            High[0] + instrumentTickSize,
                            baselineActive,
                            contextActive,
                            evidenceActive,
                            qualifiedActive,
                            "PULLBACK CONTEXTUAL",
                            true);

                    if (shortSignalCreated)
                    {
                        lastShortPullbackBar =
                            CurrentBar;

                        return;
                    }
                }
            }

            /*
            * O impulso contextual é aplicado somente
            * no modo universal.
            */
            if (!EnableUniversalRealtimeAnalysis
                || !EnableContextMomentumSignal)
            {
                return;
            }

            /*
            * =====================================================
            * IMPULSO CONTEXTUAL
            * =====================================================
            */

            const double minimumImpulseBodyAtr =
                0.30;

            const double maximumImpulseBodyAtr =
                1.30;

            const double minimumImpulseVolume =
                1.10;

            const double minimumLongCloseLocation =
                0.72;

            const double maximumShortCloseLocation =
                0.28;

            const double maximumEmaDistanceAtr =
                1.00;

            bool impulseBodyAccepted =
                candleBodyAtr
                    >= minimumImpulseBodyAtr
                && candleBodyAtr
                    <= maximumImpulseBodyAtr;

            bool impulseVolumeAccepted =
                relativeVolume
                    >= minimumImpulseVolume;

            bool impulseDistanceAccepted =
                distanceFromFastEmaAtr
                    <= maximumEmaDistanceAtr;

            /*
            * Compra por impulso.
            */

            bool longBreakout =
                Close[0] > High[1]
                && High[0] > High[1];

            bool longImpulseCandle =
                Close[0] > Open[0]
                && closeLocation
                    >= minimumLongCloseLocation;

            bool longVwapAlignment =
                Close[0] > sessionVwap
                && sessionVwap
                    >= previousSessionVwap;

            bool longImpulseConfirmation =
                longTrend
                && longBreakout
                && longImpulseCandle
                && longVwapAlignment
                && impulseBodyAccepted
                && impulseVolumeAccepted
                && impulseDistanceAccepted;

            if (EnableLongSignals
                && longImpulseConfirmation
                && PassesRealtimeDeltaFilter(
                    SignalDirection.Long)
                && CurrentBar - lastLongPullbackBar
                    >= PullbackCooldownBars)
            {
                double longImpulseStop =
                    Math.Min(
                        Low[0],
                        Low[1])
                    - instrumentTickSize;

                if (longImpulseStop
                    < Close[0] - instrumentTickSize)
                {
                    bool longImpulseCreated =
                        RegisterPullbackCandidates(
                            SignalDirection.Long,
                            longImpulseStop,
                            baselineActive,
                            contextActive,
                            evidenceActive,
                            qualifiedActive,
                            "IMPULSO CONTEXTUAL",
                            false);

                    if (longImpulseCreated)
                    {
                        lastLongPullbackBar =
                            CurrentBar;

                        return;
                    }
                }
            }

            /*
            * Venda por impulso.
            */

            bool shortBreakout =
                Close[0] < Low[1]
                && Low[0] < Low[1];

            bool shortImpulseCandle =
                Close[0] < Open[0]
                && closeLocation
                    <= maximumShortCloseLocation;

            bool shortVwapAlignment =
                Close[0] < sessionVwap
                && sessionVwap
                    <= previousSessionVwap;

            bool shortImpulseConfirmation =
                shortTrend
                && shortBreakout
                && shortImpulseCandle
                && shortVwapAlignment
                && impulseBodyAccepted
                && impulseVolumeAccepted
                && impulseDistanceAccepted;

            if (EnableShortSignals
                && shortImpulseConfirmation
                && PassesRealtimeDeltaFilter(
                    SignalDirection.Short)
                && CurrentBar - lastShortPullbackBar
                    >= PullbackCooldownBars)
            {
                double shortImpulseStop =
                    Math.Max(
                        High[0],
                        High[1])
                    + instrumentTickSize;

                if (shortImpulseStop
                    > Close[0] + instrumentTickSize)
                {
                    bool shortImpulseCreated =
                        RegisterPullbackCandidates(
                            SignalDirection.Short,
                            shortImpulseStop,
                            baselineActive,
                            contextActive,
                            evidenceActive,
                            qualifiedActive,
                            "IMPULSO CONTEXTUAL",
                            false);

                    if (shortImpulseCreated)
                    {
                        lastShortPullbackBar =
                            CurrentBar;
                    }
                }
            }
        }

        private bool RegisterPullbackCandidates(
            SignalDirection direction,
            double technicalStopPrice,
            bool baselineActive,
            bool contextActive,
            bool evidenceActive,
            bool qualifiedActive,
            string universalDecisionSummary =
                "PULLBACK CONTEXTUAL",
            bool requireFullContext = true)
        {
            SignalContext context =
                BuildSignalContext(direction);

            if (EnableUniversalRealtimeAnalysis)
            {
                /*
                * Pullback:
                * exige aprovação completa do MarketContextAnalyzer.
                *
                * Impulso:
                * já possui filtros próprios de tendência,
                * VWAP, candle, volume, distância e rompimento.
                * Por isso aceita contexto parcial com pelo
                * menos quatro confirmações.
                */
                bool contextAccepted =
                    requireFullContext
                        ? context.Passed
                        : context.Score >= 4;

                if (!contextAccepted
                    || contextActive)
                {
                    return false;
                }

                SignalContext realtimeContext =
                    context.WithDecision(
                        true,
                        universalDecisionSummary);

                TradeSignal realtimeSignal =
                    signalAnalyzer.CreatePullback(
                        SignalSetup.ContextPullback,
                        direction,
                        Close[0],
                        technicalStopPrice,
                        RiskRewardRatio,
                        instrumentTickSize,
                        instrumentPointValue,
                        Time[0],
                        ValidForBars,
                        realtimeContext);

                return RegisterAndRenderSignal(
                    realtimeSignal,
                    true);
            }

            bool signalCreated = false;

            if (!baselineActive)
            {
                TradeSignal baselineSignal =
                    signalAnalyzer.CreatePullback(
                        SignalSetup.TrendPullback,
                        direction,
                        Close[0],
                        technicalStopPrice,
                        RiskRewardRatio,
                        instrumentTickSize,
                        instrumentPointValue,
                        Time[0],
                        ValidForBars,
                        context);

                signalCreated =
                    RegisterAndRenderSignal(
                        baselineSignal,
                        ShouldRenderSetup(
                            SignalSetup.TrendPullback))
                    || signalCreated;
            }

            if (context.Passed
                && !contextActive)
            {
                TradeSignal contextSignal =
                    signalAnalyzer.CreatePullback(
                        SignalSetup.ContextPullback,
                        direction,
                        Close[0],
                        technicalStopPrice,
                        RiskRewardRatio,
                        instrumentTickSize,
                        instrumentPointValue,
                        Time[0],
                        ValidForBars,
                        context);

                signalCreated =
                    RegisterAndRenderSignal(
                        contextSignal,
                        ShouldRenderSetup(
                            SignalSetup.ContextPullback))
                    || signalCreated;
            }

            if (!evidenceActive
                && ValidationPlan.MatchesEvidenceRule(
                    Bars.Instrument.FullName,
                    direction,
                    Close[0],
                    context))
            {
                SignalContext evidenceContext =
                    context.WithDecision(
                        true,
                        "REGRA POR EVIDENCIA APROVADA");

                TradeSignal evidenceSignal =
                    signalAnalyzer.CreatePullback(
                        SignalSetup.EvidencePullback,
                        direction,
                        Close[0],
                        technicalStopPrice,
                        RiskRewardRatio,
                        instrumentTickSize,
                        instrumentPointValue,
                        Time[0],
                        ValidForBars,
                        evidenceContext);

                signalCreated =
                    RegisterAndRenderSignal(
                        evidenceSignal,
                        ShouldRenderSetup(
                            SignalSetup.EvidencePullback))
                    || signalCreated;
            }

            if (!qualifiedActive
                && ValidationPlan.MatchesQualifiedRule(
                    Bars.Instrument.FullName,
                    direction,
                    context))
            {
                SignalContext qualifiedContext =
                    context.WithDecision(
                        true,
                        "REGRA 149D APROVADA");

                TradeSignal qualifiedSignal =
                    signalAnalyzer.CreatePullback(
                        SignalSetup.QualifiedPullback,
                        direction,
                        Close[0],
                        technicalStopPrice,
                        ValidationPlan.QualifiedTargetR,
                        instrumentTickSize,
                        instrumentPointValue,
                        Time[0],
                        ValidationPlan
                            .QualifiedValidForBars,
                        qualifiedContext);

                signalCreated =
                    RegisterAndRenderSignal(
                        qualifiedSignal,
                        ShouldRenderSetup(
                            SignalSetup.QualifiedPullback))
                    || signalCreated;
            }

            return signalCreated;
        }
        private SignalContext BuildSignalContext(
            SignalDirection direction)
        {
            double normalizedAtr =
                atr[0] > 0
                    ? atr[0]
                    : instrumentTickSize;

            int slopeLookback =
                ValidationPlan.EmaSlopeLookbackBars;

            double fastSlopeAtr =
                (fastEma[0]
                    - fastEma[slopeLookback])
                / normalizedAtr;

            double slowSlopeAtr =
                (slowEma[0]
                    - slowEma[slopeLookback])
                / normalizedAtr;

            double candleRange =
                High[0] - Low[0];

            double closeLocation =
                candleRange > 0
                    ? (Close[0] - Low[0])
                        / candleRange
                    : 0.5;

            double candleBodyAtr =
                Math.Abs(
                    Close[0] - Open[0])
                / normalizedAtr;

            double averageVolume =
                volumeAverage[0];

            double relativeVolume =
                averageVolume > 0
                    ? Volume[0] / averageVolume
                    : 0;

            return marketContextAnalyzer.Analyze(
                direction,
                Close[0],
                sessionVwap,
                previousSessionVwap,
                fastSlopeAtr,
                slowSlopeAtr,
                sessionHigh,
                sessionLow,
                candleBodyAtr,
                closeLocation,
                relativeVolume,
                normalizedAtr);
        }

        private void UpdateSessionContext()
        {
            double typicalPrice =
                (High[0] + Low[0] + Close[0])
                / 3.0;

            double barVolume =
                Math.Max(
                    0,
                    Volume[0]);

            if (Bars.IsFirstBarOfSession
                || cumulativeSessionVolume <= 0)
            {
                cumulativeSessionPriceVolume = 0;
                cumulativeSessionVolume = 0;

                sessionHigh = High[0];
                sessionLow = Low[0];

                previousSessionVwap =
                    typicalPrice;

                sessionVwap =
                    typicalPrice;
            }
            else
            {
                previousSessionVwap =
                    sessionVwap;

                sessionHigh =
                    Math.Max(
                        sessionHigh,
                        High[0]);

                sessionLow =
                    Math.Min(
                        sessionLow,
                        Low[0]);
            }

            cumulativeSessionPriceVolume +=
                typicalPrice * barVolume;

            cumulativeSessionVolume +=
                barVolume;

            if (cumulativeSessionVolume > 0)
            {
                sessionVwap =
                    cumulativeSessionPriceVolume
                    / cumulativeSessionVolume;
            }
        }

        private void EvaluateBaselineSetup()
        {
            if (!EnableBaselineComparison
                || signalTracker.HasActiveSignal(
                    SignalSetup.EmaCrossBaseline))
            {
                return;
            }

            if (EnableLongSignals
                && CrossAbove(
                    fastEma,
                    slowEma,
                    1))
            {
                RegisterAndRenderSignal(
                    signalAnalyzer.CreateEmaCross(
                        SignalDirection.Long,
                        Close[0],
                        atr[0],
                        StopAtrMultiplier,
                        RiskRewardRatio,
                        instrumentTickSize,
                        instrumentPointValue,
                        Time[0],
                        ValidForBars),
                    ShouldRenderSetup(
                        SignalSetup.EmaCrossBaseline));
            }
            else if (EnableShortSignals
                && CrossBelow(
                    fastEma,
                    slowEma,
                    1))
            {
                RegisterAndRenderSignal(
                    signalAnalyzer.CreateEmaCross(
                        SignalDirection.Short,
                        Close[0],
                        atr[0],
                        StopAtrMultiplier,
                        RiskRewardRatio,
                        instrumentTickSize,
                        instrumentPointValue,
                        Time[0],
                        ValidForBars),
                    ShouldRenderSetup(
                        SignalSetup.EmaCrossBaseline));
            }
        }

        private bool RegisterAndRenderSignal(
            TradeSignal signal,
            bool renderOnChart)
        {
            if (signal == null)
                return false;

            if (ShouldRejectByRisk(signal))
            {
                TrackedSignal rejectedSignal =
                    signalTracker.RejectByRisk(
                        signal,
                        CurrentBar);

                if (rejectedSignal != null)
                {
                    RecordSignal(
                        rejectedSignal);
                }

                if (renderOnChart)
                {
                    RenderRejectedSignal(
                        signal);
                }

                return false;
            }

            TrackedSignal trackedSignal =
                signalTracker.Register(
                    signal,
                    CurrentBar);

            if (trackedSignal == null)
                return false;

            RecordSignal(trackedSignal);

            if (renderOnChart)
                RenderSignal(signal);

            return true;
        }

        private bool ShouldRejectByRisk(
            TradeSignal signal)
        {
            if (signal == null)
                return true;

            if (signal.Setup
                    == SignalSetup.QualifiedPullback
                && !ValidationPlan
                    .IsQualifiedRiskEligible(
                        signal.RiskCurrency))
            {
                return true;
            }

            /*
            * O limite financeiro agora também vale
            * para o modo universal.
            */
            return RiskLimitPolicy
                    == RiskLimitMode
                        .DescartarAcimaDoLimite
                && MaximumRiskPerContract > 0
                && signal.RiskCurrency
                    > MaximumRiskPerContract;
        }

        private void RecordSignal(
            TrackedSignal trackedSignal)
        {
            if (signalJournal == null)
                return;

            IList<TrackedSignal> signals =
                signalTracker.GetSignals();

            if (!signalJournal.SynchronizeDay(
                signals,
                trackedSignal.Signal.CreatedAt.Date))
            {
                Print(
                    "Trade Assistant: não foi possível "
                    + "gravar o histórico CSV. "
                    + signalJournal.LastError);
            }

            if (validationSummaryJournal != null
                && !validationSummaryJournal
                    .Record(signals))
            {
                Print(
                    "Trade Assistant: não foi possível "
                    + "gravar o resumo diário. "
                    + validationSummaryJournal
                        .LastError);
            }

            if (validationSegmentJournal != null
                && !validationSegmentJournal
                    .Record(signals))
            {
                Print(
                    "Trade Assistant: nao foi possivel "
                    + "gravar a analise segmentada. "
                    + validationSegmentJournal
                        .LastError);
            }
        }

        private bool ShouldRenderSetup(
            SignalSetup setup)
        {
            if (EnableUniversalRealtimeAnalysis)
            {
                return setup
                    == SignalSetup.ContextPullback;
            }

            if (!EnableValidationMode)
            {
                return setup
                    == SignalSetup.TrendPullback;
            }

            return ValidationPlan.GetProfile(
                Bars.Instrument.FullName,
                setup,
                RiskRewardRatio)
                .RenderOnChart;
        }

        private bool IsUniversalRealtimeTimeframe()
        {
            return BarsPeriod.BarsPeriodType
                    == BarsPeriodType.Minute
                && BarsPeriod.Value
                    == UniversalRealtimePlan
                        .RequiredMinutePeriod;
        }

        private bool IsInsideRealtimeWindow(
            DateTime time)
        {
            return UniversalRealtimePlan
                .IsInsideWindow(
                    time,
                    RealtimeStartTime,
                    RealtimeEndTime);
        }

        private bool PassesRealtimeDeltaFilter(
            SignalDirection direction)
        {
            /*
            * O delta permanece sendo coletado e exibido no painel,
            * mas temporariamente não bloqueia sinais.
            *
            * Primeiro precisamos validar se o snapshot está alinhado
            * corretamente ao candle de cinco minutos.
            */
            return true;
        }

        private void RenderModePanel()
        {
            if (EnableUniversalRealtimeAnalysis)
            {
                RenderUniversalRealtimePanel();
                return;
            }

            if (intradayMomentumTracker != null)
            {
                string momentumPanel =
                    "TRADE ASSISTANT v"
                    + TradeAssistantVersion.Current
                    + " | RESULTADO HIPOTETICO "
                    + "| SEM ORDENS"
                    + GetIntradayMomentumPanelText();

                Draw.TextFixed(
                    this,
                    "TradeAssistant.Mode",
                    momentumPanel,
                    TextPosition.TopRight,
                    Brushes.WhiteSmoke,
                    new SimpleFont(
                        "Segoe UI Semibold",
                        12),
                    Brushes.SlateGray,
                    Brushes.Black,
                    78);

                return;
            }

            ValidationProfile profile =
                ValidationPlan.GetPrimaryProfile(
                    Bars.Instrument.FullName,
                    RiskRewardRatio);

            ValidationStatistics statistics =
                ValidationStatisticsCalculator
                    .Calculate(
                        signalTracker.GetSignals(),
                        profile.Setup,
                        profile.TargetR,
                        Time[0].Date);

            TrackedSignal lastSignal =
                signalTracker.GetLastSignal(
                    profile.Setup);

            string currentStatus =
                lastSignal == null
                    ? "AGUARDANDO SINAL"
                    : GetValidationStatusText(
                        lastSignal,
                        profile.TargetR);

            string configurationStatus =
                !EnableValidationMode
                    ? "DESLIGADO"
                    : validationConfigurationMatches
                        ? "CONGELADA / VÁLIDA"
                        : "DIVERGENTE - NOVOS "
                            + "SINAIS BLOQUEADOS";

            string sampleStatus =
                ValidationPlan.IsForwardSample(
                    Time[0])
                    ? "AMOSTRA PROSPECTIVA"
                    : "REFERÊNCIA HISTÓRICA - "
                        + "REVISÃO INICIA "
                        + ValidationPlan
                            .ForwardStartDate
                            .ToString("dd/MM");

            string signalDetails =
                lastSignal == null
                    ? "Nenhum sinal registrado"
                    : string.Format(
                        "{0}\n"
                        + "Entrada: {1}\n"
                        + "Stop: {2}\n"
                        + "Alvo da validação: "
                        + "{3} ({9:N1}R)\n"
                        + "Distância: {4:N2} pts "
                        + "| {5:N0} ticks\n"
                        + "Risco 1 contrato "
                        + "(sem custos): {6}\n"
                        + "Retorno da validação "
                        + "(sem custos): {7}\n"
                        + "Limite: {8}\n"
                        + "Medição: 1R {10} | "
                        + "1,5R {11} | 2R {12}\n"
                        + "Primeiro evento: {13}",
                        lastSignal.Signal.Direction
                            == SignalDirection.Long
                                ? "COMPRA"
                                : "VENDA",
                        FormatPrice(
                            lastSignal.Signal
                                .EntryPrice),
                        FormatPrice(
                            lastSignal.Signal
                                .StopPrice),
                        FormatPrice(
                            GetValidationTargetPrice(
                                lastSignal.Signal,
                                profile.TargetR)),
                        lastSignal.Signal.Risk,
                        lastSignal.Signal.RiskTicks,
                        FormatCurrency(
                            lastSignal.Signal
                                .RiskCurrency),
                        FormatCurrency(
                            lastSignal.Signal
                                .RiskCurrency
                            * profile.TargetR),
                        GetRiskLimitStatus(
                            lastSignal.Signal),
                        profile.TargetR,
                        GetComparisonStatusText(
                            lastSignal
                                .TargetOneRStatus),
                        GetComparisonStatusText(
                            lastSignal
                                .TargetOnePointFiveRStatus),
                        GetComparisonStatusText(
                            lastSignal
                                .TargetTwoRStatus),
                        GetFirstEventText(
                            lastSignal.FirstEvent));

            if (lastSignal != null
                && (lastSignal.Signal.Setup
                        == SignalSetup.ContextPullback
                    || lastSignal.Signal.Setup
                        == SignalSetup.EvidencePullback
                    || lastSignal.Signal.Setup
                        == SignalSetup.QualifiedPullback))
            {
                signalDetails += string.Format(
                    "\nContexto: {0}/6 | "
                    + "VWAP {1} | "
                    + "Dist. {2:N2} ATR | "
                    + "Vol. {3:N2}x\n"
                    + "{4}",
                    lastSignal.Signal.Context.Score,
                    FormatPrice(
                        lastSignal.Signal.Context
                            .SessionVwap),
                    lastSignal.Signal.Context
                        .VwapDistanceAtr,
                    lastSignal.Signal.Context
                        .RelativeVolume,
                    lastSignal.Signal.Context
                        .Summary);
            }

            string panelText =
                string.Format(
                    "TRADE ASSISTANT v"
                    + TradeAssistantVersion.Current
                    + " | RESULTADO HIPOTÉTICO "
                    + "| SEM ORDENS\n"
                    + "Rodada: "
                    + ValidationPlan.RoundId
                    + " | {21}\n"
                    + "Setup: {12} | "
                    + "Etapa: {13} | "
                    + "Alvo: {14:N1}R\n"
                    + "Configuração: {15}\n"
                    + "Status: {0}\n"
                    + "Histórico e resumo: {11}\n\n"
                    + "{1}\n\n"
                    + "Hoje: {2} sinais | "
                    + "{3} ativos | "
                    + "{16} decididos\n"
                    + "Alvos: {4} | Stops: {5}\n"
                    + "Expirados: {6} | "
                    + "Ambíguos: {7}\n"
                    + "Descartados por risco: {10}\n"
                    + "Acerto: {8:N1}% | "
                    + "Resultado: "
                    + "{9:+0.00;-0.00;0.00} R "
                    + "| {17}\n"
                    + "Sequência máx. de stops: "
                    + "{18} | Drawdown: "
                    + "{19:N2} R / {20}",
                    currentStatus,
                    signalDetails,
                    statistics.Total,
                    statistics.Active,
                    statistics.TargetHits,
                    statistics.StopHits,
                    statistics.Expired,
                    statistics.Ambiguous,
                    statistics.WinRate,
                    statistics.ResultR,
                    statistics.RiskRejected,
                    GetJournalStatus(),
                    GetSetupText(
                        profile.Setup,
                        profile.Stage),
                    GetValidationStageText(
                        profile.Stage),
                    profile.TargetR,
                    configurationStatus,
                    statistics.Decided,
                    FormatCurrency(
                        statistics.ResultCurrency),
                    statistics
                        .MaximumConsecutiveLosses,
                    statistics
                        .MaximumDrawdownR,
                    FormatCurrency(
                        statistics
                            .MaximumDrawdownCurrency),
                    sampleStatus);

            panelText +=
                GetIntradayMomentumPanelText();

            Draw.TextFixed(
                this,
                "TradeAssistant.Mode",
                panelText,
                TextPosition.TopRight,
                Brushes.WhiteSmoke,
                new SimpleFont(
                    "Segoe UI Semibold",
                    12),
                Brushes.SlateGray,
                Brushes.Black,
                78);
        }

        private void RenderUniversalRealtimePanel()
        {
            bool supportedTimeframe =
                IsUniversalRealtimeTimeframe();

            bool insideWindow =
                IsInsideRealtimeWindow(
                    Time[0]);

            ValidationStatistics statistics =
                ValidationStatisticsCalculator
                    .Calculate(
                        signalTracker.GetSignals(),
                        SignalSetup.ContextPullback,
                        RiskRewardRatio,
                        Time[0].Date);

            TrackedSignal lastSignal =
                signalTracker.GetLastSignal(
                    SignalSetup.ContextPullback);

            string status =
                !supportedTimeframe
                    ? "USE GRAFICO DE 5 MINUTOS"
                    : !insideWindow
                        ? "FORA DA JANELA DE ANALISE"
                        : lastSignal == null
                            ? "ANALISANDO EM TEMPO REAL"
                            : GetValidationStatusText(
                                lastSignal,
                                RiskRewardRatio);

            string details =
                lastSignal == null
                    ? "Aguardando pullback ou rompimento "
                        + "com tendencia, VWAP, candle "
                        + "e volume"
                    : string.Format(
                        "{0}\n"
                        + "Entrada: {1}\n"
                        + "Stop: {2}\n"
                        + "Alvo: {3} ({4:N1}R)\n"
                        + "Risco de 1 contrato: "
                        + "{5} | {6}\n"
                        + "Contexto: {7}/6 | "
                        + "VWAP {8}\n"
                        + "{9}",
                        lastSignal.Signal.Direction
                            == SignalDirection.Long
                                ? "COMPRA"
                                : "VENDA",
                        FormatPrice(
                            lastSignal.Signal
                                .EntryPrice),
                        FormatPrice(
                            lastSignal.Signal
                                .StopPrice),
                        FormatPrice(
                            GetValidationTargetPrice(
                                lastSignal.Signal,
                                RiskRewardRatio)),
                        RiskRewardRatio,
                        FormatCurrency(
                            lastSignal.Signal
                                .RiskCurrency),
                        GetRiskLimitStatus(
                            lastSignal.Signal),
                        lastSignal.Signal.Context
                            .Score,
                        FormatPrice(
                            lastSignal.Signal.Context
                                .SessionVwap),
                        lastSignal.Signal.Context
                            .Summary);

            string deltaStatus;

            if (latestDeltaSnapshot == null)
            {
                deltaStatus =
                    "Delta: aguardando dados "
                    + "em tempo real";
            }
            else
            {
                bool longDeltaConfirmed =
                    UniversalRealtimePlan
                        .IsDeltaConfirmed(
                            true,
                            latestDeltaSnapshot
                                .ClassifiedVolume,
                            latestDeltaSnapshot
                                .DeltaPercentage);

                bool shortDeltaConfirmed =
                    UniversalRealtimePlan
                        .IsDeltaConfirmed(
                            false,
                            latestDeltaSnapshot
                                .ClassifiedVolume,
                            latestDeltaSnapshot
                                .DeltaPercentage);

                string deltaDirection =
                    longDeltaConfirmed
                        ? "COMPRA CONFIRMADA"
                        : shortDeltaConfirmed
                            ? "VENDA CONFIRMADA"
                            : "SEM CONFIRMACAO";

                deltaStatus = string.Format(
                    "Delta {0:HH:mm}: "
                    + "{1:+0;-0;0} | "
                    + "{2:+0.0;-0.0;0.0}%\n"
                    + "BUY {3} | SELL {4} | "
                    + "UNK {5} | {6}",
                    latestDeltaSnapshot.BarTime,
                    latestDeltaSnapshot.Delta,
                    latestDeltaSnapshot
                        .DeltaPercentage,
                    latestDeltaSnapshot.BuyVolume,
                    latestDeltaSnapshot.SellVolume,
                    latestDeltaSnapshot.UnknownVolume,
                    deltaDirection);
            }

            string panelText =
                string.Format(
                    "TRADE ASSISTANT v{0} | "
                    + "TEMPO REAL | SEM ORDENS\n"
                    + "ESTRATEGIA: FLUXO CONTEXTUAL\n"
                    + "Pullback: {20} | Impulso: {21}\n"
                    + "Aprovacao deste ativo: {1}\n"
                    + "Ativo: {2} | "
                    + "Grafico: {3}-{4}\n"
                    + "Janela: {5:00}:{6:00} - "
                    + "{7:00}:{8:00} BRT\n"
                    + "Status: {9}\n\n"
                    + "{10}\n\n"
                    + "{19}\n\n"
                    + "Hoje: {11} sinais | "
                    + "{12} ativos | "
                    + "Alvos {13} | "
                    + "Stops {14} | "
                    + "Expirados {15}\n"
                    + "Resultado hipotetico: "
                    + "{16:+0.00;-0.00;0.00} R "
                    + "| {17}\n"
                    + "CSV: {18}",
                    TradeAssistantVersion.Current,
                    UniversalRealtimePlan
                        .ApprovalStatus,
                    Bars.Instrument.FullName,
                    BarsPeriod.BarsPeriodType,
                    BarsPeriod.Value,
                    RealtimeStartTime / 10000,
                    (RealtimeStartTime / 100) % 100,
                    RealtimeEndTime / 10000,
                    (RealtimeEndTime / 100) % 100,
                    status,
                    details,
                    statistics.Total,
                    statistics.Active,
                    statistics.TargetHits,
                    statistics.StopHits,
                    statistics.Expired,
                    statistics.ResultR,
                    FormatCurrency(
                        statistics.ResultCurrency),
                    GetJournalStatus(),
                    deltaStatus,
                    EnableContextPullbackSignal
                        ? "ATIVO"
                        : "DESLIGADO",
                    EnableContextMomentumSignal
                        ? "ATIVO"
                        : "DESLIGADO");

            Draw.TextFixed(
                this,
                "TradeAssistant.Mode",
                panelText,
                TextPosition.TopRight,
                Brushes.WhiteSmoke,
                new SimpleFont(
                    "Segoe UI Semibold",
                    12),
                Brushes.DarkSlateGray,
                Brushes.Black,
                78);
        }

        private string GetIntradayMomentumPanelText()
        {
            if (!EnableIntradayMomentumCandidate)
            {
                return "\n\nMOMENTUM INTRADIARIO: "
                    + "DESLIGADO";
            }

            if (intradayMomentumTracker == null)
            {
                return "\n\nMOMENTUM INTRADIARIO: "
                    + "DISPONIVEL SOMENTE PARA MNQ";
            }

            IList<IntradayMomentumTrade> trades =
                intradayMomentumTracker.GetTrades();

            int active = 0;
            int completed = 0;
            double resultCurrency = 0;

            foreach (
                IntradayMomentumTrade trade
                in trades)
            {
                if (trade.IsActive)
                {
                    active++;
                }
                else
                {
                    completed++;
                    resultCurrency +=
                        trade.ResultCurrency;
                }
            }

            IntradayMomentumTrade lastTrade =
                intradayMomentumTracker.LastTrade;

            string status =
                lastTrade == null
                    ? "AQUECIMENTO / AGUARDANDO "
                        + "20 SESSOES"
                    : lastTrade.IsActive
                        ? (lastTrade.Direction
                            == SignalDirection.Long
                                ? "COMPRA HIPOTETICA ATIVA"
                                : "VENDA HIPOTETICA ATIVA")
                        : lastTrade.Status
                            == IntradayMomentumStatus
                                .StopHit
                            ? "ULTIMA ANALISE: STOP"
                            : "ULTIMA ANALISE: "
                                + "FECHAMENTO";

            string details =
                lastTrade == null
                    ? "Sem operacao registrada"
                    : string.Format(
                        "{0} | Entrada {1} | "
                        + "Stop {2}\n"
                        + "Regime: {3} | "
                        + "Sinal: "
                        + "{4:+0.00;-0.00;0.00} pts",
                        lastTrade.Direction
                            == SignalDirection.Long
                                ? "COMPRA"
                                : "VENDA",
                        FormatPrice(
                            lastTrade.EntryPrice),
                        FormatPrice(
                            lastTrade.StopPrice),
                        lastTrade.Regime
                            == IntradayMomentumRegime
                                .HighOpeningVolatility
                            ? "VOLATILIDADE ALTA"
                            : "VOLATILIDADE BAIXA",
                        lastTrade.CompositeSignal);

            return string.Format(
                "\n\nMOMENTUM INTRADIARIO "
                + "| 1 MICRO MNQ "
                + "| SOMENTE OBSERVACAO\n"
                + "Rodada: {0} | "
                + "Risco: USD {1:N2} "
                + "+ custo USD {2:N2}\n"
                + "Fase atual: {10}\n"
                + "Status: {3}\n"
                + "{4}\n"
                + "Amostra carregada: {5} | "
                + "Ativas: {6} | "
                + "Encerradas: {7} | "
                + "Resultado: USD "
                + "{8:+0.00;-0.00;0.00}\n"
                + "CSV proprio: {9}",
                IntradayMomentumPlan.RoundId,
                IntradayMomentumPlan
                    .RiskCurrencyPerMicro,
                IntradayMomentumPlan
                    .RoundTurnCostCurrency,
                status,
                details,
                trades.Count,
                active,
                completed,
                resultCurrency,
                GetIntradayMomentumJournalStatus(),
                IntradayMomentumPlan
                    .GetSamplePhase(Time[0]));
        }

        private string GetIntradayMomentumJournalStatus()
        {
            if (!EnableCsvJournal)
                return "DESLIGADO";

            if (intradayMomentumJournal == null)
                return "INDISPONIVEL";

            return string.IsNullOrEmpty(
                intradayMomentumJournal.LastError)
                    ? "ATIVO"
                    : "ERRO";
        }

        private string GetJournalStatus()
        {
            if (!EnableCsvJournal)
                return "DESLIGADO";

            if (signalJournal == null)
                return "INDISPONÍVEL";

            bool journalOk =
                string.IsNullOrEmpty(
                    signalJournal.LastError);

            if (EnableUniversalRealtimeAnalysis)
                return journalOk ? "ATIVO" : "ERRO";

            bool summaryOk =
                validationSummaryJournal != null
                && string.IsNullOrEmpty(
                    validationSummaryJournal
                        .LastError);

            bool segmentOk =
                validationSegmentJournal != null
                && string.IsNullOrEmpty(
                    validationSegmentJournal
                        .LastError);

            return journalOk
                && summaryOk
                && segmentOk
                    ? "ATIVO"
                    : "ERRO";
        }

        private static string GetSetupText(
            SignalSetup setup,
            ValidationStage stage)
        {
            if (setup
                == SignalSetup.EmaCrossBaseline)
            {
                return "CRUZAMENTO EMA";
            }

            if (setup
                == SignalSetup.ContextPullback)
            {
                return "PULLBACK CONTEXTUAL";
            }

            if (setup
                == SignalSetup.EvidencePullback)
            {
                return "VENDA POR EVIDÊNCIA";
            }

            if (setup
                == SignalSetup.QualifiedPullback)
            {
                return stage
                        == ValidationStage.Candidate
                    ? "MNQ VENDA VALIDADA 149D"
                    : "SEM CANDIDATO PARA "
                        + "ESTE ATIVO";
            }

            return "PULLBACK BASE";
        }

        private static string GetValidationStageText(
            ValidationStage stage)
        {
            switch (stage)
            {
                case ValidationStage.Candidate:
                    return "CANDIDATO EM VALIDAÇÃO";

                case ValidationStage.Observation:
                    return "OBSERVAÇÃO";

                case ValidationStage.Paused:
                    return "PAUSADO";

                default:
                    return "REFERÊNCIA";
            }
        }

        private static double GetValidationTargetPrice(
            TradeSignal signal,
            double targetR)
        {
            if (Math.Abs(targetR - 1.0)
                < 0.0000001)
            {
                return signal.TargetOneRPrice;
            }

            if (Math.Abs(targetR - 1.5)
                < 0.0000001)
            {
                return signal
                    .TargetOnePointFiveRPrice;
            }

            if (Math.Abs(targetR - 2.0)
                < 0.0000001)
            {
                return signal.TargetTwoRPrice;
            }

            return signal.Direction
                    == SignalDirection.Long
                ? signal.EntryPrice
                    + (signal.Risk * targetR)
                : signal.EntryPrice
                    - (signal.Risk * targetR);
        }

        private string GetRiskLimitStatus(
            TradeSignal signal)
        {
            if (signal.Setup
                    == SignalSetup.QualifiedPullback
                && signal.RiskCurrency
                    < ValidationPlan
                        .FrozenMinimumRiskPerContract)
            {
                return "ABAIXO DO MÍNIMO";
            }

            if (MaximumRiskPerContract <= 0)
                return "NÃO CONFIGURADO";

            return signal.RiskCurrency
                    <= MaximumRiskPerContract
                ? "DENTRO DO LIMITE"
                : "ACIMA DO LIMITE";
        }

        private static string GetComparisonStatusText(
            ComparisonStatus status)
        {
            switch (status)
            {
                case ComparisonStatus.TargetHit:
                    return "ALVO";

                case ComparisonStatus.StopHit:
                    return "STOP";

                case ComparisonStatus.Expired:
                    return "EXPIRADO";

                case ComparisonStatus.Ambiguous:
                    return "AMBÍGUO";

                case ComparisonStatus.RiskRejected:
                    return "DESCARTADO";

                default:
                    return "PENDENTE";
            }
        }

        private static string GetFirstEventText(
            FirstOutcomeEvent firstEvent)
        {
            switch (firstEvent)
            {
                case FirstOutcomeEvent.Target1R:
                    return "ALVO 1R";

                case FirstOutcomeEvent.Stop:
                    return "STOP";

                case FirstOutcomeEvent.Expired:
                    return "EXPIRAÇÃO";

                case FirstOutcomeEvent.Ambiguous:
                    return "AMBÍGUO";

                case FirstOutcomeEvent.RiskRejected:
                    return "DESCARTADO";

                default:
                    return "PENDENTE";
            }
        }

        private string FormatCurrency(
            double value)
        {
            return instrumentCurrency
                + " "
                + value.ToString("N2");
        }

        private static string GetCurrencyCode(
            string currencyName)
        {
            switch (currencyName)
            {
                case "UsDollar":
                    return "USD";

                case "Euro":
                    return "EUR";

                case "BritishPound":
                    return "GBP";

                case "JapaneseYen":
                    return "JPY";

                case "SwissFranc":
                    return "CHF";

                default:
                    return currencyName;
            }
        }

        private void RenderOutcome(
            TrackedSignal trackedSignal)
        {
            if (!visibleSignalIds.Contains(
                trackedSignal.Signal.Id))
            {
                return;
            }

            ValidationProfile profile =
                ValidationPlan.GetProfile(
                    Bars.Instrument.FullName,
                    trackedSignal.Signal.Setup,
                    RiskRewardRatio);

            double displayTargetR =
                EnableUniversalRealtimeAnalysis
                && trackedSignal.Signal.Setup
                    == SignalSetup.ContextPullback
                    ? RiskRewardRatio
                    : profile.TargetR;

            ComparisonStatus validationStatus =
                ValidationStatisticsCalculator
                    .GetStatus(
                        trackedSignal,
                        displayTargetR);

            string text;
            Brush brush;
            double price;

            switch (validationStatus)
            {
                case ComparisonStatus.TargetHit:
                    text = "ALVO";
                    brush = Brushes.ForestGreen;

                    price =
                        GetValidationTargetPrice(
                            trackedSignal.Signal,
                            displayTargetR);

                    break;

                case ComparisonStatus.StopHit:
                    text = "STOP";
                    brush = Brushes.Firebrick;

                    price =
                        trackedSignal.Signal
                            .StopPrice;

                    break;

                case ComparisonStatus.Expired:
                    text = "EXPIRADO";
                    brush = Brushes.DimGray;

                    price =
                        trackedSignal.Signal
                            .EntryPrice;

                    break;

                default:
                    text = "AMBIGUO";
                    brush = Brushes.DarkOrange;

                    price =
                        trackedSignal.Signal
                            .EntryPrice;

                    break;
            }

            Draw.Text(
                this,
                "TradeAssistant."
                    + trackedSignal.Signal.Id
                    + ".Outcome",
                text,
                0,
                price,
                brush);
        }

        private void RenderIntradayMomentum(
            IntradayMomentumTrade trade)
        {
            bool isLong =
                trade.Direction
                == SignalDirection.Long;

            Brush directionBrush =
                isLong
                    ? Brushes.DeepSkyBlue
                    : Brushes.DarkOrange;

            double markerPrice =
                isLong
                    ? trade.EntryPrice
                        - (instrumentTickSize * 2)
                    : trade.EntryPrice
                        + (instrumentTickSize * 2);

            string tagPrefix =
                "TradeAssistant." + trade.Id;

            PrepareVisualHistory(trade.Id);

            if (isLong)
            {
                Draw.ArrowUp(
                    this,
                    tagPrefix + ".Arrow",
                    false,
                    trade.SignalTime,
                    markerPrice,
                    directionBrush);
            }
            else
            {
                Draw.ArrowDown(
                    this,
                    tagPrefix + ".Arrow",
                    false,
                    trade.SignalTime,
                    markerPrice,
                    directionBrush);
            }

            Draw.Rectangle(
                this,
                tagPrefix + ".RiskZone",
                false,
                trade.SignalTime,
                trade.EntryPrice,
                trade.ScheduledExitTime,
                trade.StopPrice,
                Brushes.Transparent,
                Brushes.IndianRed,
                ZoneOpacity);

            Draw.Line(
                this,
                tagPrefix + ".Entry",
                false,
                trade.SignalTime,
                trade.EntryPrice,
                trade.ScheduledExitTime,
                trade.EntryPrice,
                Brushes.DodgerBlue,
                DashStyleHelper.Solid,
                2);

            Draw.Line(
                this,
                tagPrefix + ".Stop",
                false,
                trade.SignalTime,
                trade.StopPrice,
                trade.ScheduledExitTime,
                trade.StopPrice,
                Brushes.IndianRed,
                DashStyleHelper.Dash,
                2);

            Draw.Text(
                this,
                tagPrefix + ".EntryLabel",
                (isLong ? "COMPRA" : "VENDA")
                    + " MOMENTUM "
                    + FormatPrice(
                        trade.EntryPrice),
                0,
                trade.EntryPrice,
                directionBrush);

            Draw.Text(
                this,
                tagPrefix + ".StopLabel",
                "STOP USD "
                    + trade.RiskCurrency
                        .ToString("N2")
                    + " "
                    + FormatPrice(
                        trade.StopPrice),
                0,
                trade.StopPrice,
                Brushes.IndianRed);
        }

        private void RenderIntradayMomentumOutcome(
            IntradayMomentumTrade trade)
        {
            if (!visibleSignalIds.Contains(
                    trade.Id)
                || !trade.ExitTime.HasValue
                || !trade.ExitPrice.HasValue)
            {
                return;
            }

            bool profitable =
                trade.ResultCurrency > 0;

            string text =
                trade.Status
                    == IntradayMomentumStatus.StopHit
                    ? "STOP USD "
                        + trade.ResultCurrency
                            .ToString("N2")
                    : "FECHAMENTO USD "
                        + trade.ResultCurrency
                            .ToString(
                                "+0.00;-0.00;0.00");

            Draw.Text(
                this,
                "TradeAssistant."
                    + trade.Id
                    + ".Outcome",
                text,
                0,
                trade.ExitPrice.Value,
                profitable
                    ? Brushes.ForestGreen
                    : Brushes.Firebrick);
        }

        private static string GetValidationStatusText(
            TrackedSignal trackedSignal,
            double targetR)
        {
            ComparisonStatus status =
                ValidationStatisticsCalculator
                    .GetStatus(
                        trackedSignal,
                        targetR);

            switch (status)
            {
                case ComparisonStatus.TargetHit:
                    return "ALVO DA VALIDAÇÃO "
                        + "ATINGIDO";

                case ComparisonStatus.StopHit:
                    return "STOP ATINGIDO";

                case ComparisonStatus.Expired:
                    return "SINAL EXPIRADO";

                case ComparisonStatus.Ambiguous:
                    return "RESULTADO AMBÍGUO";

                case ComparisonStatus.RiskRejected:
                    return "DESCARTADO POR RISCO";

                default:
                    string direction =
                        trackedSignal.Signal.Direction
                            == SignalDirection.Long
                            ? "COMPRA"
                            : "VENDA";

                    int remainingBars =
                        Math.Max(
                            0,
                            trackedSignal.Signal
                                .ValidForBars
                            - trackedSignal
                                .BarsElapsed);

                    return direction
                        + " ATIVA ("
                        + remainingBars
                        + " candles)";
            }
        }

        private static string GetStatusText(
            TrackedSignal trackedSignal)
        {
            if (trackedSignal.Status
                == SignalStatus.Active)
            {
                string direction =
                    trackedSignal.Signal.Direction
                        == SignalDirection.Long
                        ? "COMPRA"
                        : "VENDA";

                int remainingBars =
                    Math.Max(
                        0,
                        trackedSignal.Signal
                            .ValidForBars
                        - trackedSignal.BarsElapsed);

                return direction
                    + " ATIVA ("
                    + remainingBars
                    + " candles)";
            }

            switch (trackedSignal.Status)
            {
                case SignalStatus.TargetHit:
                    return "ALVO ATINGIDO";

                case SignalStatus.StopHit:
                    return "STOP ATINGIDO";

                case SignalStatus.Expired:
                    return "SINAL EXPIRADO";

                case SignalStatus.RiskRejected:
                    return "DESCARTADO POR RISCO";

                default:
                    return "RESULTADO AMBIGUO";
            }
        }

        private void RenderRejectedSignal(
            TradeSignal signal)
        {
            bool isLong =
                signal.Direction
                == SignalDirection.Long;

            double markerPrice =
                isLong
                    ? Low[0] - (TickSize * 2)
                    : High[0] + (TickSize * 2);

            string tagPrefix =
                "TradeAssistant." + signal.Id;

            PrepareVisualHistory(signal.Id);

            Draw.Text(
                this,
                tagPrefix + ".Rejected",
                "DESCARTADO: RISCO "
                    + FormatCurrency(
                        signal.RiskCurrency),
                0,
                markerPrice,
                Brushes.DimGray);
        }

        private void RenderSignal(
            TradeSignal signal)
        {
            ValidationProfile profile =
                ValidationPlan.GetProfile(
                    Bars.Instrument.FullName,
                    signal.Setup,
                    RiskRewardRatio);

            double displayTargetR =
                EnableUniversalRealtimeAnalysis
                && signal.Setup
                    == SignalSetup.ContextPullback
                    ? RiskRewardRatio
                    : profile.TargetR;

            double validationTargetPrice =
                GetValidationTargetPrice(
                    signal,
                    displayTargetR);

            bool isLong =
                signal.Direction
                == SignalDirection.Long;

            Brush directionBrush =
                isLong
                    ? Brushes.DeepSkyBlue
                    : Brushes.DarkOrange;

            double markerPrice =
                isLong
                    ? Low[0] - (TickSize * 2)
                    : High[0] + (TickSize * 2);

            string tagPrefix =
                "TradeAssistant." + signal.Id;

            PrepareVisualHistory(signal.Id);

            if (isLong)
            {
                Draw.ArrowUp(
                    this,
                    tagPrefix + ".Arrow",
                    false,
                    0,
                    markerPrice,
                    directionBrush);
            }
            else
            {
                Draw.ArrowDown(
                    this,
                    tagPrefix + ".Arrow",
                    false,
                    0,
                    markerPrice,
                    directionBrush);
            }

            Draw.Rectangle(
                this,
                tagPrefix + ".RiskZone",
                false,
                0,
                signal.EntryPrice,
                -signal.ValidForBars,
                signal.StopPrice,
                Brushes.Transparent,
                Brushes.IndianRed,
                ZoneOpacity);

            Draw.Rectangle(
                this,
                tagPrefix + ".RewardZone",
                false,
                0,
                signal.EntryPrice,
                -signal.ValidForBars,
                validationTargetPrice,
                Brushes.Transparent,
                Brushes.MediumSeaGreen,
                ZoneOpacity);

            Draw.Line(
                this,
                tagPrefix + ".Entry",
                false,
                0,
                signal.EntryPrice,
                -signal.ValidForBars,
                signal.EntryPrice,
                Brushes.DodgerBlue,
                DashStyleHelper.Solid,
                2);

            Draw.Line(
                this,
                tagPrefix + ".Stop",
                false,
                0,
                signal.StopPrice,
                -signal.ValidForBars,
                signal.StopPrice,
                Brushes.IndianRed,
                DashStyleHelper.Dash,
                2);

            Draw.Line(
                this,
                tagPrefix + ".Target",
                false,
                0,
                validationTargetPrice,
                -signal.ValidForBars,
                validationTargetPrice,
                Brushes.MediumSeaGreen,
                DashStyleHelper.Dash,
                2);

            string entryLabel =
                "ENTRADA "
                + FormatPrice(
                    signal.EntryPrice);

            if (signal.Setup
                    == SignalSetup.ContextPullback
                || signal.Setup
                    == SignalSetup.EvidencePullback
                || signal.Setup
                    == SignalSetup.QualifiedPullback)
            {
                entryLabel +=
                    " | CONTEXTO "
                    + signal.Context.Score
                    + "/6";
            }

            Draw.Text(
                this,
                tagPrefix + ".EntryLabel",
                entryLabel,
                -signal.ValidForBars,
                signal.EntryPrice,
                Brushes.DodgerBlue);

            Draw.Text(
                this,
                tagPrefix + ".StopLabel",
                "STOP "
                    + FormatPrice(
                        signal.StopPrice),
                -signal.ValidForBars,
                signal.StopPrice,
                Brushes.IndianRed);

            Draw.Text(
                this,
                tagPrefix + ".TargetLabel",
                "ALVO "
                    + displayTargetR
                        .ToString("N1")
                    + "R "
                    + FormatPrice(
                        validationTargetPrice),
                -signal.ValidForBars,
                validationTargetPrice,
                Brushes.MediumSeaGreen);
        }

        private string FormatPrice(
            double price)
        {
            return Bars.Instrument
                .MasterInstrument
                .FormatPrice(price);
        }

        private void PrepareVisualHistory(
            string signalId)
        {
            visualSignalIds.Enqueue(
                signalId);

            visibleSignalIds.Add(
                signalId);

            int visualLimit =
                EnableUniversalRealtimeAnalysis
                    ? int.MaxValue
                    : ShowHistoricalSignals
                        ? MaxHistoricalSignals
                        : 1;

            while (visualSignalIds.Count
                > visualLimit)
            {
                RemoveSignalVisuals(
                    visualSignalIds.Dequeue());
            }
        }

        private void RemoveSignalVisuals(
            string signalId)
        {
            string tagPrefix =
                "TradeAssistant." + signalId;

            string[] suffixes =
            {
                ".Arrow",
                ".RiskZone",
                ".RewardZone",
                ".Entry",
                ".Stop",
                ".Target",
                ".EntryLabel",
                ".StopLabel",
                ".TargetLabel",
                ".Rejected",
                ".Outcome"
            };

            foreach (string suffix in suffixes)
            {
                RemoveDrawObject(
                    tagPrefix + suffix);
            }

            visibleSignalIds.Remove(
                signalId);
        }

        #region Properties

        [NinjaScriptProperty]
        [Display(
            Name = "Exibir compras",
            GroupName = "Sinais",
            Order = 1)]
        public bool EnableLongSignals
        {
            get;
            set;
        }

        [NinjaScriptProperty]
        [Display(
            Name = "Exibir vendas",
            GroupName = "Sinais",
            Order = 2)]
        public bool EnableShortSignals
        {
            get;
            set;
        }

        [NinjaScriptProperty]
        [Display(
            Name = "Ativar pullback experimental",
            Description =
                "Exibe e acompanha sinais de "
                + "pullback a favor da tendência.",
            GroupName = "Sinais",
            Order = 3)]
        public bool EnablePullbackSignals
        {
            get;
            set;
        }

        [NinjaScriptProperty]
        [Display(
            Name = "Ativar pullback contextual",
            Description =
                "Ativa o gatilho de retorno à EMA. "
                + "Pode ser desligado sem desativar "
                + "o impulso contextual.",
            GroupName = "Sinais",
            Order = 4)]
        public bool EnableContextPullbackSignal
        {
            get;
            set;
        }

        [NinjaScriptProperty]
        [Display(
            Name = "Ativar impulso contextual",
            Description =
                "Ativa rompimentos qualificados "
                + "com tendência, VWAP, candle "
                + "e volume.",
            GroupName = "Sinais",
            Order = 5)]
        public bool EnableContextMomentumSignal
        {
            get;
            set;
        }

        [NinjaScriptProperty]
        [Display(
            Name = "Registrar comparação EMA",
            Description =
                "Registra o cruzamento de EMA "
                + "somente no CSV, sem desenhar "
                + "no gráfico.",
            GroupName = "Sinais",
            Order = 4)]
        public bool EnableBaselineComparison
        {
            get;
            set;
        }

        [NinjaScriptProperty]
        [Range(
            2,
            int.MaxValue)]
        [Display(
            Name = "EMA rápida",
            GroupName = "Análise",
            Order = 1)]
        public int FastEmaPeriod
        {
            get;
            set;
        }

        [NinjaScriptProperty]
        [Range(
            3,
            int.MaxValue)]
        [Display(
            Name = "EMA lenta",
            GroupName = "Análise",
            Order = 2)]
        public int SlowEmaPeriod
        {
            get;
            set;
        }

        [NinjaScriptProperty]
        [Range(
            0,
            2)]
        [Display(
            Name = "Tolerância do pullback (ATR)",
            Description =
                "Distância máxima da EMA rápida "
                + "como fração do ATR.",
            GroupName = "Análise",
            Order = 3)]
        public double PullbackToleranceAtr
        {
            get;
            set;
        }

        [NinjaScriptProperty]
        [Range(
            1,
            50)]
        [Display(
            Name = "Intervalo entre pullbacks",
            Description =
                "Quantidade mínima de candles "
                + "entre candidatos da mesma "
                + "direção.",
            GroupName = "Análise",
            Order = 4)]
        public int PullbackCooldownBars
        {
            get;
            set;
        }

        [NinjaScriptProperty]
        [Range(
            2,
            int.MaxValue)]
        [Display(
            Name = "Período ATR",
            GroupName = "Risco",
            Order = 1)]
        public int AtrPeriod
        {
            get;
            set;
        }

        [NinjaScriptProperty]
        [Range(
            0.1,
            double.MaxValue)]
        [Display(
            Name = "Multiplicador do stop",
            GroupName = "Risco",
            Order = 2)]
        public double StopAtrMultiplier
        {
            get;
            set;
        }

        [NinjaScriptProperty]
        [Range(
            0.1,
            double.MaxValue)]
        [Display(
            Name = "Relação risco/retorno",
            GroupName = "Risco",
            Order = 3)]
        public double RiskRewardRatio
        {
            get;
            set;
        }

        [NinjaScriptProperty]
        [Range(
            0,
            double.MaxValue)]
        [Display(
            Name = "Risco máximo por contrato",
            Description =
                "Use 0 para não configurar "
                + "limite financeiro.",
            GroupName = "Risco",
            Order = 4)]
        public double MaximumRiskPerContract
        {
            get;
            set;
        }

        [NinjaScriptProperty]
        [Display(
            Name = "Política do limite",
            Description =
                "Apenas avisa ou descarta sinais "
                + "acima do limite financeiro.",
            GroupName = "Risco",
            Order = 5)]
        public RiskLimitMode RiskLimitPolicy
        {
            get;
            set;
        }

        [NinjaScriptProperty]
        [Range(
            1,
            20)]
        [Display(
            Name = "Validade em candles",
            GroupName = "Sinais",
            Order = 5)]
        public int ValidForBars
        {
            get;
            set;
        }

        [NinjaScriptProperty]
        [Display(
            Name = "Exibir sinais anteriores",
            Description =
                "No modo universal, todos os "
                + "sinais dos dias carregados "
                + "permanecem visiveis.",
            GroupName = "Visual",
            Order = 1)]
        public bool ShowHistoricalSignals
        {
            get;
            set;
        }

        [NinjaScriptProperty]
        [Range(
            1,
            20)]
        [Display(
            Name = "Máximo de sinais no gráfico",
            GroupName = "Visual",
            Order = 2)]
        public int MaxHistoricalSignals
        {
            get;
            set;
        }

        [NinjaScriptProperty]
        [Range(
            0,
            100)]
        [Display(
            Name = "Opacidade das zonas",
            GroupName = "Visual",
            Order = 3)]
        public int ZoneOpacity
        {
            get;
            set;
        }

        [NinjaScriptProperty]
        [Display(
            Name = "Salvar histórico CSV",
            GroupName = "Histórico",
            Order = 1)]
        public bool EnableCsvJournal
        {
            get;
            set;
        }

        [NinjaScriptProperty]
        [Display(
            Name = "Modo de validação 0.8",
            Description =
                "Aplica o plano congelado por "
                + "ativo e bloqueia novos sinais "
                + "se a configuração divergir.",
            GroupName = "Validação",
            Order = 1)]
        public bool EnableValidationMode
        {
            get;
            set;
        }

        [NinjaScriptProperty]
        [Display(
            Name =
                "Analise universal em tempo real",
            Description =
                "Analisa qualquer futuro em "
                + "grafico de 5 minutos. "
                + "Modo experimental, hipotetico "
                + "e sem ordens.",
            GroupName = "Tempo real",
            Order = 1)]
        public bool EnableUniversalRealtimeAnalysis
        {
            get;
            set;
        }

        [NinjaScriptProperty]
        [Range(
            0,
            235959)]
        [Display(
            Name =
                "Inicio da analise (HHmmss)",
            GroupName = "Tempo real",
            Order = 2)]
        public int RealtimeStartTime
        {
            get;
            set;
        }

        [NinjaScriptProperty]
        [Range(
            0,
            235959)]
        [Display(
            Name =
                "Fim da analise (HHmmss)",
            GroupName = "Tempo real",
            Order = 3)]
        public int RealtimeEndTime
        {
            get;
            set;
        }

        [NinjaScriptProperty]
        [Display(
            Name = "Momentum intradiario MNQ",
            Description =
                "Ativa o novo candidato somente "
                + "visual, sem enviar ordens. "
                + "Usa uma serie interna de "
                + "1 minuto.",
            GroupName = "Validacao",
            Order = 2)]
        public bool EnableIntradayMomentumCandidate
        {
            get;
            set;
        }

        #endregion
    }
}