using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.TradeAssistant.Analysis;
using NinjaTrader.NinjaScript.TradeAssistant.Configuration;
using NinjaTrader.NinjaScript.TradeAssistant.Models;
using NinjaTrader.NinjaScript.TradeAssistant.Persistence;
using NinjaTrader.NinjaScript.TradeAssistant.Tracking;

namespace NinjaTrader.NinjaScript.Indicators
{
    public class TradeAnalysisAssistant : Indicator
    {
        private ATR atr;
        private EMA fastEma;
        private string instrumentCurrency;
        private double instrumentPointValue;
        private double instrumentTickSize;
        private int lastLongPullbackBar;
        private int lastShortPullbackBar;
        private EMA slowEma;
        private HashSet<string> visibleSignalIds;
        private Queue<string> visualSignalIds;
        private SignalAnalyzer signalAnalyzer;
        private CsvSignalJournal signalJournal;
        private CsvValidationSummaryJournal validationSummaryJournal;
        private SignalTracker signalTracker;
        private bool validationConfigurationMatches;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Indicador visual de análise hipotética sem execução automática de ordens. Versão " + TradeAssistantVersion.Current + ".";
                Name = "Trade Analysis Assistant";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                IsChartOnly = true;
                DisplayInDataBox = false;
                DrawOnPricePanel = true;

                EnableLongSignals = true;
                EnableShortSignals = true;
                EnablePullbackSignals = true;
                EnableBaselineComparison = true;
                FastEmaPeriod = 9;
                SlowEmaPeriod = 21;
                AtrPeriod = 14;
                PullbackToleranceAtr = 0.1;
                PullbackCooldownBars = 3;
                StopAtrMultiplier = 1.5;
                RiskRewardRatio = 2.0;
                ValidForBars = 3;
                ShowHistoricalSignals = false;
                MaxHistoricalSignals = 5;
                ZoneOpacity = 14;
                EnableCsvJournal = true;
                EnableValidationMode = true;
                MaximumRiskPerContract = 75;
                RiskLimitPolicy = RiskLimitMode.DescartarAcimaDoLimite;
            }
            else if (State == State.DataLoaded)
            {
                fastEma = EMA(FastEmaPeriod);
                slowEma = EMA(SlowEmaPeriod);
                atr = ATR(AtrPeriod);
                instrumentCurrency = GetCurrencyCode(Bars.Instrument.MasterInstrument.Currency.ToString());
                instrumentPointValue = Bars.Instrument.MasterInstrument.PointValue;
                instrumentTickSize = Bars.Instrument.MasterInstrument.TickSize;
                signalAnalyzer = new SignalAnalyzer();
                signalTracker = new SignalTracker();
                validationConfigurationMatches = ValidationPlan.MatchesFrozenConfiguration(
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
                    string journalDirectory = System.IO.Path.Combine(
                        NinjaTrader.Core.Globals.UserDataDir,
                        "TradeAssistant",
                        "Data");
                    string barsPeriodDescription = BarsPeriod.BarsPeriodType + "-" + BarsPeriod.Value;
                    signalJournal = new CsvSignalJournal(
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
                        RiskLimitPolicy.ToString());
                    validationSummaryJournal = new CsvValidationSummaryJournal(
                        System.IO.Path.Combine(
                            NinjaTrader.Core.Globals.UserDataDir,
                            "TradeAssistant",
                            "Summaries"),
                        Bars.Instrument.FullName,
                        barsPeriodDescription,
                        TradeAssistantVersion.Current,
                        instrumentCurrency,
                        RiskRewardRatio);
                }
                visibleSignalIds = new HashSet<string>();
                visualSignalIds = new Queue<string>();
            }
        }

        protected override void OnBarUpdate()
        {
            int requiredBars = Math.Max(SlowEmaPeriod, AtrPeriod) + 1;
            if (CurrentBar < requiredBars)
                return;

            foreach (TrackedSignal closedSignal in signalTracker.Update(High[0], Low[0], Time[0], CurrentBar))
            {
                RecordSignal(closedSignal);
                RenderOutcome(closedSignal);
            }

            if (!EnableValidationMode || validationConfigurationMatches)
            {
                EvaluatePullbackSetup();
                EvaluateBaselineSetup();
            }

            RenderModePanel();
        }

        private void EvaluatePullbackSetup()
        {
            if (!EnablePullbackSignals || signalTracker.HasActiveSignal(SignalSetup.TrendPullback))
                return;

            double tolerance = atr[0] * PullbackToleranceAtr;
            bool longTrend = fastEma[0] > slowEma[0]
                && fastEma[0] > fastEma[1]
                && slowEma[0] >= slowEma[1];
            bool longConfirmation = Low[0] <= fastEma[0] + tolerance
                && Close[0] > fastEma[0]
                && Close[0] > Open[0];

            if (EnableLongSignals
                && longTrend
                && longConfirmation
                && CurrentBar - lastLongPullbackBar >= PullbackCooldownBars)
            {
                lastLongPullbackBar = CurrentBar;
                RegisterAndRenderSignal(signalAnalyzer.CreatePullback(
                    SignalDirection.Long,
                    Close[0],
                    Low[0] - instrumentTickSize,
                    RiskRewardRatio,
                    instrumentTickSize,
                    instrumentPointValue,
                    Time[0],
                    ValidForBars),
                    ShouldRenderSetup(SignalSetup.TrendPullback));
                return;
            }

            bool shortTrend = fastEma[0] < slowEma[0]
                && fastEma[0] < fastEma[1]
                && slowEma[0] <= slowEma[1];
            bool shortConfirmation = High[0] >= fastEma[0] - tolerance
                && Close[0] < fastEma[0]
                && Close[0] < Open[0];

            if (EnableShortSignals
                && shortTrend
                && shortConfirmation
                && CurrentBar - lastShortPullbackBar >= PullbackCooldownBars)
            {
                lastShortPullbackBar = CurrentBar;
                RegisterAndRenderSignal(signalAnalyzer.CreatePullback(
                    SignalDirection.Short,
                    Close[0],
                    High[0] + instrumentTickSize,
                    RiskRewardRatio,
                    instrumentTickSize,
                    instrumentPointValue,
                    Time[0],
                    ValidForBars),
                    ShouldRenderSetup(SignalSetup.TrendPullback));
            }
        }

        private void EvaluateBaselineSetup()
        {
            if (!EnableBaselineComparison || signalTracker.HasActiveSignal(SignalSetup.EmaCrossBaseline))
                return;

            if (EnableLongSignals && CrossAbove(fastEma, slowEma, 1))
                RegisterAndRenderSignal(signalAnalyzer.CreateEmaCross(
                    SignalDirection.Long,
                    Close[0],
                    atr[0],
                    StopAtrMultiplier,
                    RiskRewardRatio,
                    instrumentTickSize,
                    instrumentPointValue,
                    Time[0],
                    ValidForBars),
                    ShouldRenderSetup(SignalSetup.EmaCrossBaseline));

            else if (EnableShortSignals && CrossBelow(fastEma, slowEma, 1))
                RegisterAndRenderSignal(signalAnalyzer.CreateEmaCross(
                    SignalDirection.Short,
                    Close[0],
                    atr[0],
                    StopAtrMultiplier,
                    RiskRewardRatio,
                    instrumentTickSize,
                    instrumentPointValue,
                    Time[0],
                    ValidForBars),
                    ShouldRenderSetup(SignalSetup.EmaCrossBaseline));
        }

        private void RegisterAndRenderSignal(TradeSignal signal, bool renderOnChart)
        {
            if (ShouldRejectByRisk(signal))
            {
                TrackedSignal rejectedSignal = signalTracker.RejectByRisk(signal, CurrentBar);
                RecordSignal(rejectedSignal);
                if (renderOnChart)
                    RenderRejectedSignal(signal);
                return;
            }

            TrackedSignal trackedSignal = signalTracker.Register(signal, CurrentBar);
            if (trackedSignal == null)
                return;

            RecordSignal(trackedSignal);
            if (renderOnChart)
                RenderSignal(signal);
        }

        private bool ShouldRejectByRisk(TradeSignal signal)
        {
            return RiskLimitPolicy == RiskLimitMode.DescartarAcimaDoLimite
                && MaximumRiskPerContract > 0
                && signal.RiskCurrency > MaximumRiskPerContract;
        }

        private void RecordSignal(TrackedSignal trackedSignal)
        {
            if (signalJournal == null)
                return;

            if (!signalJournal.Record(trackedSignal))
                Print("Trade Assistant: não foi possível gravar o histórico CSV. " + signalJournal.LastError);

            if (validationSummaryJournal != null
                && !validationSummaryJournal.Record(signalTracker.GetSignals()))
            {
                Print("Trade Assistant: não foi possível gravar o resumo diário. "
                    + validationSummaryJournal.LastError);
            }
        }

        private bool ShouldRenderSetup(SignalSetup setup)
        {
            if (!EnableValidationMode)
                return setup == SignalSetup.TrendPullback;

            return ValidationPlan.GetProfile(
                Bars.Instrument.FullName,
                setup,
                RiskRewardRatio).RenderOnChart;
        }

        private void RenderModePanel()
        {
            ValidationProfile profile = ValidationPlan.GetPrimaryProfile(
                Bars.Instrument.FullName,
                RiskRewardRatio);
            ValidationStatistics statistics = ValidationStatisticsCalculator.Calculate(
                signalTracker.GetSignals(),
                profile.Setup,
                profile.TargetR,
                Time[0].Date);
            TrackedSignal lastSignal = signalTracker.GetLastSignal(profile.Setup);
            string currentStatus = lastSignal == null
                ? "AGUARDANDO SINAL"
                : GetValidationStatusText(lastSignal, profile.TargetR);
            string configurationStatus = !EnableValidationMode
                ? "DESLIGADO"
                : validationConfigurationMatches
                    ? "CONGELADA / VÁLIDA"
                    : "DIVERGENTE - NOVOS SINAIS BLOQUEADOS";
            string sampleStatus = ValidationPlan.IsForwardSample(Time[0])
                ? "AMOSTRA PROSPECTIVA"
                : "REFERÊNCIA HISTÓRICA - REVISÃO INICIA 23/07";
            string signalDetails = lastSignal == null
                ? "Nenhum sinal registrado"
                : string.Format(
                    "{0}\nEntrada: {1}\nStop: {2}\nAlvo da validação: {3} ({9:N1}R)\nDistância: {4:N2} pts | {5:N0} ticks\nRisco 1 contrato (sem custos): {6}\nRetorno da validação (sem custos): {7}\nLimite: {8}\nMedição: 1R {10} | 1,5R {11} | 2R {12}\nPrimeiro evento: {13}",
                    lastSignal.Signal.Direction == SignalDirection.Long ? "COMPRA" : "VENDA",
                    FormatPrice(lastSignal.Signal.EntryPrice),
                    FormatPrice(lastSignal.Signal.StopPrice),
                    FormatPrice(GetValidationTargetPrice(lastSignal.Signal, profile.TargetR)),
                    lastSignal.Signal.Risk,
                    lastSignal.Signal.RiskTicks,
                    FormatCurrency(lastSignal.Signal.RiskCurrency),
                    FormatCurrency(lastSignal.Signal.RiskCurrency * profile.TargetR),
                    GetRiskLimitStatus(lastSignal.Signal),
                    profile.TargetR,
                    GetComparisonStatusText(lastSignal.TargetOneRStatus),
                    GetComparisonStatusText(lastSignal.TargetOnePointFiveRStatus),
                    GetComparisonStatusText(lastSignal.TargetTwoRStatus),
                    GetFirstEventText(lastSignal.FirstEvent));
            string panelText = string.Format(
                "TRADE ASSISTANT v" + TradeAssistantVersion.Current + " | RESULTADO HIPOTÉTICO | SEM ORDENS\nRodada: " + ValidationPlan.RoundId + " | {21}\nSetup: {12} | Etapa: {13} | Alvo: {14:N1}R\nConfiguração: {15}\nStatus: {0}\nHistórico e resumo: {11}\n\n{1}\n\nHoje: {2} sinais | {3} ativos | {16} decididos\nAlvos: {4} | Stops: {5}\nExpirados: {6} | Ambíguos: {7}\nDescartados por risco: {10}\nAcerto: {8:N1}% | Resultado: {9:+0.00;-0.00;0.00} R | {17}\nSequência máx. de stops: {18} | Drawdown: {19:N2} R / {20}",
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
                GetSetupText(profile.Setup),
                GetValidationStageText(profile.Stage),
                profile.TargetR,
                configurationStatus,
                statistics.Decided,
                FormatCurrency(statistics.ResultCurrency),
                statistics.MaximumConsecutiveLosses,
                statistics.MaximumDrawdownR,
                FormatCurrency(statistics.MaximumDrawdownCurrency),
                sampleStatus);

            Draw.TextFixed(
                this,
                "TradeAssistant.Mode",
                panelText,
                TextPosition.TopRight,
                Brushes.WhiteSmoke,
                new SimpleFont("Segoe UI Semibold", 12),
                Brushes.SlateGray,
                Brushes.Black,
                78);
        }

        private string GetJournalStatus()
        {
            if (!EnableCsvJournal)
                return "DESLIGADO";
            if (signalJournal == null)
                return "INDISPONÍVEL";
            bool journalOk = string.IsNullOrEmpty(signalJournal.LastError);
            bool summaryOk = validationSummaryJournal != null
                && string.IsNullOrEmpty(validationSummaryJournal.LastError);
            return journalOk && summaryOk ? "ATIVO" : "ERRO";
        }

        private static string GetSetupText(SignalSetup setup)
        {
            return setup == SignalSetup.EmaCrossBaseline
                ? "CRUZAMENTO EMA"
                : "PULLBACK";
        }

        private static string GetValidationStageText(ValidationStage stage)
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

        private static double GetValidationTargetPrice(TradeSignal signal, double targetR)
        {
            if (Math.Abs(targetR - 1.0) < 0.0000001)
                return signal.TargetOneRPrice;
            if (Math.Abs(targetR - 1.5) < 0.0000001)
                return signal.TargetOnePointFiveRPrice;
            return signal.TargetTwoRPrice;
        }

        private string GetRiskLimitStatus(TradeSignal signal)
        {
            if (MaximumRiskPerContract <= 0)
                return "NÃO CONFIGURADO";

            return signal.RiskCurrency <= MaximumRiskPerContract
                ? "DENTRO DO LIMITE"
                : "ACIMA DO LIMITE";
        }

        private static string GetComparisonStatusText(ComparisonStatus status)
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

        private static string GetFirstEventText(FirstOutcomeEvent firstEvent)
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

        private string FormatCurrency(double value)
        {
            return instrumentCurrency + " " + value.ToString("N2");
        }

        private static string GetCurrencyCode(string currencyName)
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

        private void RenderOutcome(TrackedSignal trackedSignal)
        {
            if (!visibleSignalIds.Contains(trackedSignal.Signal.Id))
                return;

            ValidationProfile profile = ValidationPlan.GetProfile(
                Bars.Instrument.FullName,
                trackedSignal.Signal.Setup,
                RiskRewardRatio);
            ComparisonStatus validationStatus = ValidationStatisticsCalculator.GetStatus(
                trackedSignal,
                profile.TargetR);
            string text;
            Brush brush;
            double price;

            switch (validationStatus)
            {
                case ComparisonStatus.TargetHit:
                    text = "ALVO";
                    brush = Brushes.ForestGreen;
                    price = GetValidationTargetPrice(trackedSignal.Signal, profile.TargetR);
                    break;
                case ComparisonStatus.StopHit:
                    text = "STOP";
                    brush = Brushes.Firebrick;
                    price = trackedSignal.Signal.StopPrice;
                    break;
                case ComparisonStatus.Expired:
                    text = "EXPIRADO";
                    brush = Brushes.DimGray;
                    price = trackedSignal.Signal.EntryPrice;
                    break;
                default:
                    text = "AMBIGUO";
                    brush = Brushes.DarkOrange;
                    price = trackedSignal.Signal.EntryPrice;
                    break;
            }

            Draw.Text(
                this,
                "TradeAssistant." + trackedSignal.Signal.Id + ".Outcome",
                text,
                0,
                price,
                brush);
        }

        private static string GetValidationStatusText(TrackedSignal trackedSignal, double targetR)
        {
            ComparisonStatus status = ValidationStatisticsCalculator.GetStatus(trackedSignal, targetR);
            switch (status)
            {
                case ComparisonStatus.TargetHit:
                    return "ALVO DA VALIDAÇÃO ATINGIDO";
                case ComparisonStatus.StopHit:
                    return "STOP ATINGIDO";
                case ComparisonStatus.Expired:
                    return "SINAL EXPIRADO";
                case ComparisonStatus.Ambiguous:
                    return "RESULTADO AMBÍGUO";
                case ComparisonStatus.RiskRejected:
                    return "DESCARTADO POR RISCO";
                default:
                    string direction = trackedSignal.Signal.Direction == SignalDirection.Long ? "COMPRA" : "VENDA";
                    int remainingBars = Math.Max(0, trackedSignal.Signal.ValidForBars - trackedSignal.BarsElapsed);
                    return direction + " ATIVA (" + remainingBars + " candles)";
            }
        }

        private static string GetStatusText(TrackedSignal trackedSignal)
        {
            if (trackedSignal.Status == SignalStatus.Active)
            {
                string direction = trackedSignal.Signal.Direction == SignalDirection.Long ? "COMPRA" : "VENDA";
                int remainingBars = Math.Max(0, trackedSignal.Signal.ValidForBars - trackedSignal.BarsElapsed);
                return direction + " ATIVA (" + remainingBars + " candles)";
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

        private void RenderRejectedSignal(TradeSignal signal)
        {
            bool isLong = signal.Direction == SignalDirection.Long;
            double markerPrice = isLong
                ? Low[0] - (TickSize * 2)
                : High[0] + (TickSize * 2);
            string tagPrefix = "TradeAssistant." + signal.Id;

            PrepareVisualHistory(signal.Id);

            Draw.Text(
                this,
                tagPrefix + ".Rejected",
                "DESCARTADO: RISCO " + FormatCurrency(signal.RiskCurrency),
                0,
                markerPrice,
                Brushes.DimGray);
        }

        private void RenderSignal(TradeSignal signal)
        {
            ValidationProfile profile = ValidationPlan.GetProfile(
                Bars.Instrument.FullName,
                signal.Setup,
                RiskRewardRatio);
            double validationTargetPrice = GetValidationTargetPrice(signal, profile.TargetR);
            bool isLong = signal.Direction == SignalDirection.Long;
            Brush directionBrush = isLong ? Brushes.DeepSkyBlue : Brushes.DarkOrange;
            double markerPrice = isLong
                ? Low[0] - (TickSize * 2)
                : High[0] + (TickSize * 2);
            string tagPrefix = "TradeAssistant." + signal.Id;

            PrepareVisualHistory(signal.Id);

            if (isLong)
                Draw.ArrowUp(this, tagPrefix + ".Arrow", false, 0, markerPrice, directionBrush);
            else
                Draw.ArrowDown(this, tagPrefix + ".Arrow", false, 0, markerPrice, directionBrush);

            Draw.Rectangle(this, tagPrefix + ".RiskZone", false, 0, signal.EntryPrice, -signal.ValidForBars, signal.StopPrice, Brushes.Transparent, Brushes.IndianRed, ZoneOpacity);
            Draw.Rectangle(this, tagPrefix + ".RewardZone", false, 0, signal.EntryPrice, -signal.ValidForBars, validationTargetPrice, Brushes.Transparent, Brushes.MediumSeaGreen, ZoneOpacity);

            Draw.Line(this, tagPrefix + ".Entry", false, 0, signal.EntryPrice, -signal.ValidForBars, signal.EntryPrice, Brushes.DodgerBlue, DashStyleHelper.Solid, 2);
            Draw.Line(this, tagPrefix + ".Stop", false, 0, signal.StopPrice, -signal.ValidForBars, signal.StopPrice, Brushes.IndianRed, DashStyleHelper.Dash, 2);
            Draw.Line(this, tagPrefix + ".Target", false, 0, validationTargetPrice, -signal.ValidForBars, validationTargetPrice, Brushes.MediumSeaGreen, DashStyleHelper.Dash, 2);

            Draw.Text(this, tagPrefix + ".EntryLabel", "ENTRADA " + FormatPrice(signal.EntryPrice), -signal.ValidForBars, signal.EntryPrice, Brushes.DodgerBlue);
            Draw.Text(this, tagPrefix + ".StopLabel", "STOP " + FormatPrice(signal.StopPrice), -signal.ValidForBars, signal.StopPrice, Brushes.IndianRed);
            Draw.Text(this, tagPrefix + ".TargetLabel", "ALVO " + profile.TargetR.ToString("N1") + "R " + FormatPrice(validationTargetPrice), -signal.ValidForBars, validationTargetPrice, Brushes.MediumSeaGreen);
        }

        private string FormatPrice(double price)
        {
            return Bars.Instrument.MasterInstrument.FormatPrice(price);
        }

        private void PrepareVisualHistory(string signalId)
        {
            visualSignalIds.Enqueue(signalId);
            visibleSignalIds.Add(signalId);

            int visualLimit = ShowHistoricalSignals ? MaxHistoricalSignals : 1;
            while (visualSignalIds.Count > visualLimit)
                RemoveSignalVisuals(visualSignalIds.Dequeue());
        }

        private void RemoveSignalVisuals(string signalId)
        {
            string tagPrefix = "TradeAssistant." + signalId;
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
                RemoveDrawObject(tagPrefix + suffix);

            visibleSignalIds.Remove(signalId);
        }

        #region Properties

        [NinjaScriptProperty]
        [Display(Name = "Exibir compras", GroupName = "Sinais", Order = 1)]
        public bool EnableLongSignals { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Exibir vendas", GroupName = "Sinais", Order = 2)]
        public bool EnableShortSignals { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Ativar pullback experimental", Description = "Exibe e acompanha sinais de pullback a favor da tendência.", GroupName = "Sinais", Order = 3)]
        public bool EnablePullbackSignals { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Registrar comparação EMA", Description = "Registra o cruzamento de EMA somente no CSV, sem desenhar no gráfico.", GroupName = "Sinais", Order = 4)]
        public bool EnableBaselineComparison { get; set; }

        [NinjaScriptProperty]
        [Range(2, int.MaxValue)]
        [Display(Name = "EMA rápida", GroupName = "Análise", Order = 1)]
        public int FastEmaPeriod { get; set; }

        [NinjaScriptProperty]
        [Range(3, int.MaxValue)]
        [Display(Name = "EMA lenta", GroupName = "Análise", Order = 2)]
        public int SlowEmaPeriod { get; set; }

        [NinjaScriptProperty]
        [Range(0, 2)]
        [Display(Name = "Tolerância do pullback (ATR)", Description = "Distância máxima da EMA rápida como fração do ATR.", GroupName = "Análise", Order = 3)]
        public double PullbackToleranceAtr { get; set; }

        [NinjaScriptProperty]
        [Range(1, 50)]
        [Display(Name = "Intervalo entre pullbacks", Description = "Quantidade mínima de candles entre candidatos da mesma direção.", GroupName = "Análise", Order = 4)]
        public int PullbackCooldownBars { get; set; }

        [NinjaScriptProperty]
        [Range(2, int.MaxValue)]
        [Display(Name = "Período ATR", GroupName = "Risco", Order = 1)]
        public int AtrPeriod { get; set; }

        [NinjaScriptProperty]
        [Range(0.1, double.MaxValue)]
        [Display(Name = "Multiplicador do stop", GroupName = "Risco", Order = 2)]
        public double StopAtrMultiplier { get; set; }

        [NinjaScriptProperty]
        [Range(0.1, double.MaxValue)]
        [Display(Name = "Relação risco/retorno", GroupName = "Risco", Order = 3)]
        public double RiskRewardRatio { get; set; }

        [NinjaScriptProperty]
        [Range(0, double.MaxValue)]
        [Display(Name = "Risco máximo por contrato", Description = "Use 0 para não configurar limite financeiro.", GroupName = "Risco", Order = 4)]
        public double MaximumRiskPerContract { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Política do limite", Description = "Apenas avisa ou descarta sinais acima do limite financeiro.", GroupName = "Risco", Order = 5)]
        public RiskLimitMode RiskLimitPolicy { get; set; }

        [NinjaScriptProperty]
        [Range(1, 20)]
        [Display(Name = "Validade em candles", GroupName = "Sinais", Order = 5)]
        public int ValidForBars { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Exibir sinais anteriores", GroupName = "Visual", Order = 1)]
        public bool ShowHistoricalSignals { get; set; }

        [NinjaScriptProperty]
        [Range(1, 20)]
        [Display(Name = "Máximo de sinais no gráfico", GroupName = "Visual", Order = 2)]
        public int MaxHistoricalSignals { get; set; }

        [NinjaScriptProperty]
        [Range(0, 100)]
        [Display(Name = "Opacidade das zonas", GroupName = "Visual", Order = 3)]
        public int ZoneOpacity { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Salvar histórico CSV", GroupName = "Histórico", Order = 1)]
        public bool EnableCsvJournal { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Modo de validação 0.8", Description = "Aplica o plano congelado por ativo e bloqueia novos sinais se a configuração divergir.", GroupName = "Validação", Order = 1)]
        public bool EnableValidationMode { get; set; }

        #endregion
    }
}
