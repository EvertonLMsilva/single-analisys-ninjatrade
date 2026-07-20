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
        private EMA slowEma;
        private HashSet<string> visibleSignalIds;
        private Queue<string> visualSignalIds;
        private SignalAnalyzer signalAnalyzer;
        private CsvSignalJournal signalJournal;
        private SignalTracker signalTracker;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Indicador visual de análise sem execução automática de ordens. Versão " + TradeAssistantVersion.Current + ".";
                Name = "Trade Analysis Assistant";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                IsChartOnly = true;
                DisplayInDataBox = false;
                DrawOnPricePanel = true;

                EnableLongSignals = true;
                EnableShortSignals = true;
                FastEmaPeriod = 9;
                SlowEmaPeriod = 21;
                AtrPeriod = 14;
                StopAtrMultiplier = 1.5;
                RiskRewardRatio = 2.0;
                ValidForBars = 3;
                ShowHistoricalSignals = false;
                MaxHistoricalSignals = 5;
                ZoneOpacity = 14;
                EnableCsvJournal = true;
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
                        instrumentCurrency,
                        MaximumRiskPerContract,
                        RiskLimitPolicy.ToString());
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

            if (!signalTracker.HasActiveSignal() && EnableLongSignals && CrossAbove(fastEma, slowEma, 1))
                RegisterAndRenderSignal(signalAnalyzer.Create(
                    SignalDirection.Long,
                    Close[0],
                    atr[0],
                    StopAtrMultiplier,
                    RiskRewardRatio,
                    instrumentTickSize,
                    instrumentPointValue,
                    Time[0],
                    ValidForBars));

            else if (!signalTracker.HasActiveSignal() && EnableShortSignals && CrossBelow(fastEma, slowEma, 1))
                RegisterAndRenderSignal(signalAnalyzer.Create(
                    SignalDirection.Short,
                    Close[0],
                    atr[0],
                    StopAtrMultiplier,
                    RiskRewardRatio,
                    instrumentTickSize,
                    instrumentPointValue,
                    Time[0],
                    ValidForBars));

            RenderModePanel();
        }

        private void RegisterAndRenderSignal(TradeSignal signal)
        {
            if (ShouldRejectByRisk(signal))
            {
                TrackedSignal rejectedSignal = signalTracker.RejectByRisk(signal, CurrentBar);
                RecordSignal(rejectedSignal);
                RenderRejectedSignal(signal);
                return;
            }

            TrackedSignal trackedSignal = signalTracker.Register(signal, CurrentBar);
            if (trackedSignal == null)
                return;

            RecordSignal(trackedSignal);
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
        }

        private void RenderModePanel()
        {
            SignalStatistics statistics = signalTracker.GetStatistics();
            TrackedSignal lastSignal = signalTracker.GetLastSignal();
            string currentStatus = lastSignal == null
                ? "AGUARDANDO SINAL"
                : GetStatusText(lastSignal);
            string signalDetails = lastSignal == null
                ? "Nenhum sinal registrado"
                : string.Format(
                    "{0}\nEntrada: {1}\nStop: {2}\nAlvo: {3}\nDistância: {4:N2} pts | {5:N0} ticks\nRisco 1 contrato (sem custos): {6}\nAlvo 1 contrato (sem custos): {7}\nLimite: {8}\nR:R: {9:N2}",
                    lastSignal.Signal.Direction == SignalDirection.Long ? "COMPRA" : "VENDA",
                    FormatPrice(lastSignal.Signal.EntryPrice),
                    FormatPrice(lastSignal.Signal.StopPrice),
                    FormatPrice(lastSignal.Signal.TargetPrice),
                    lastSignal.Signal.Risk,
                    lastSignal.Signal.RiskTicks,
                    FormatCurrency(lastSignal.Signal.RiskCurrency),
                    FormatCurrency(lastSignal.Signal.RewardCurrency),
                    GetRiskLimitStatus(lastSignal.Signal),
                    lastSignal.Signal.RiskRewardRatio);
            string panelText = string.Format(
                "TRADE ASSISTANT v" + TradeAssistantVersion.Current + " | ANALYSIS ONLY\nStatus: {0}\nHistórico CSV: {11}\n\n{1}\n\nSinais: {2} | Ativos: {3}\nAlvos: {4} | Stops: {5}\nExpirados: {6} | Ambíguos: {7}\nDescartados por risco: {10}\nAcerto: {8:N1}% | Total: {9:+0.00;-0.00;0.00} R",
                currentStatus,
                signalDetails,
                statistics.Total,
                statistics.Active,
                statistics.TargetHits,
                statistics.StopHits,
                statistics.Expired,
                statistics.Ambiguous,
                statistics.WinRate,
                statistics.TotalR,
                statistics.RiskRejected,
                GetJournalStatus());

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
            return string.IsNullOrEmpty(signalJournal.LastError) ? "ATIVO" : "ERRO";
        }

        private string GetRiskLimitStatus(TradeSignal signal)
        {
            if (MaximumRiskPerContract <= 0)
                return "NÃO CONFIGURADO";

            return signal.RiskCurrency <= MaximumRiskPerContract
                ? "DENTRO DO LIMITE"
                : "ACIMA DO LIMITE";
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

            string text;
            Brush brush;
            double price;

            switch (trackedSignal.Status)
            {
                case SignalStatus.TargetHit:
                    text = "ALVO";
                    brush = Brushes.ForestGreen;
                    price = trackedSignal.Signal.TargetPrice;
                    break;
                case SignalStatus.StopHit:
                    text = "STOP";
                    brush = Brushes.Firebrick;
                    price = trackedSignal.Signal.StopPrice;
                    break;
                case SignalStatus.Expired:
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
            Draw.Rectangle(this, tagPrefix + ".RewardZone", false, 0, signal.EntryPrice, -signal.ValidForBars, signal.TargetPrice, Brushes.Transparent, Brushes.MediumSeaGreen, ZoneOpacity);

            Draw.Line(this, tagPrefix + ".Entry", false, 0, signal.EntryPrice, -signal.ValidForBars, signal.EntryPrice, Brushes.DodgerBlue, DashStyleHelper.Solid, 2);
            Draw.Line(this, tagPrefix + ".Stop", false, 0, signal.StopPrice, -signal.ValidForBars, signal.StopPrice, Brushes.IndianRed, DashStyleHelper.Dash, 2);
            Draw.Line(this, tagPrefix + ".Target", false, 0, signal.TargetPrice, -signal.ValidForBars, signal.TargetPrice, Brushes.MediumSeaGreen, DashStyleHelper.Dash, 2);

            Draw.Text(this, tagPrefix + ".EntryLabel", "ENTRADA " + FormatPrice(signal.EntryPrice), -signal.ValidForBars, signal.EntryPrice, Brushes.DodgerBlue);
            Draw.Text(this, tagPrefix + ".StopLabel", "STOP " + FormatPrice(signal.StopPrice), -signal.ValidForBars, signal.StopPrice, Brushes.IndianRed);
            Draw.Text(this, tagPrefix + ".TargetLabel", "ALVO " + FormatPrice(signal.TargetPrice), -signal.ValidForBars, signal.TargetPrice, Brushes.MediumSeaGreen);
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
        [Range(2, int.MaxValue)]
        [Display(Name = "EMA rápida", GroupName = "Análise", Order = 1)]
        public int FastEmaPeriod { get; set; }

        [NinjaScriptProperty]
        [Range(3, int.MaxValue)]
        [Display(Name = "EMA lenta", GroupName = "Análise", Order = 2)]
        public int SlowEmaPeriod { get; set; }

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
        [Display(Name = "Validade em candles", GroupName = "Sinais", Order = 3)]
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

        #endregion
    }
}
