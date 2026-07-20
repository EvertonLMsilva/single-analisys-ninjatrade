using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.TradeAssistant.Analysis;
using NinjaTrader.NinjaScript.TradeAssistant.Models;

namespace NinjaTrader.NinjaScript.Indicators
{
    public class TradeAnalysisAssistant : Indicator
    {
        private ATR atr;
        private EMA fastEma;
        private EMA slowEma;
        private SignalAnalyzer signalAnalyzer;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Indicador visual de análise sem execução automática de ordens.";
                Name = "Trade Analysis Assistant";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = false;

                EnableLongSignals = true;
                EnableShortSignals = true;
                FastEmaPeriod = 9;
                SlowEmaPeriod = 21;
                AtrPeriod = 14;
                StopAtrMultiplier = 1.5;
                RiskRewardRatio = 2.0;
                ValidForBars = 3;
            }
            else if (State == State.DataLoaded)
            {
                fastEma = EMA(FastEmaPeriod);
                slowEma = EMA(SlowEmaPeriod);
                atr = ATR(AtrPeriod);
                signalAnalyzer = new SignalAnalyzer();

            }
        }

        protected override void OnBarUpdate()
        {
            int requiredBars = Math.Max(SlowEmaPeriod, AtrPeriod) + 1;
            if (CurrentBar < requiredBars)
                return;

            RenderModePanel();

            if (EnableLongSignals && CrossAbove(fastEma, slowEma, 1))
                RenderSignal(signalAnalyzer.Create(
                    SignalDirection.Long,
                    Close[0],
                    atr[0],
                    StopAtrMultiplier,
                    RiskRewardRatio,
                    Time[0],
                    ValidForBars));

            if (EnableShortSignals && CrossBelow(fastEma, slowEma, 1))
                RenderSignal(signalAnalyzer.Create(
                    SignalDirection.Short,
                    Close[0],
                    atr[0],
                    StopAtrMultiplier,
                    RiskRewardRatio,
                    Time[0],
                    ValidForBars));
        }

        private void RenderModePanel()
        {
            string panelText = string.Format(
                "ANALYSIS ONLY\nEMA: {0}/{1}\nATR: {2}\nR:R: {3:N1}",
                FastEmaPeriod,
                SlowEmaPeriod,
                AtrPeriod,
                RiskRewardRatio);

            Draw.TextFixed(
                this,
                "TradeAssistant.Mode",
                panelText,
                TextPosition.TopRight,
                Brushes.DimGray,
                new SimpleFont("Segoe UI", 12),
                Brushes.Transparent,
                Brushes.WhiteSmoke,
                70);
        }

        private void RenderSignal(TradeSignal signal)
        {
            bool isLong = signal.Direction == SignalDirection.Long;
            Brush directionBrush = isLong ? Brushes.ForestGreen : Brushes.Firebrick;
            string directionText = isLong ? "COMPRA POSSÍVEL" : "VENDA POSSÍVEL";
            double markerPrice = isLong
                ? Low[0] - (TickSize * 2)
                : High[0] + (TickSize * 2);
            string tagPrefix = "TradeAssistant." + signal.Id;

            if (isLong)
                Draw.ArrowUp(this, tagPrefix + ".Arrow", false, 0, markerPrice, directionBrush);
            else
                Draw.ArrowDown(this, tagPrefix + ".Arrow", false, 0, markerPrice, directionBrush);

            Draw.Line(this, tagPrefix + ".Entry", false, 0, signal.EntryPrice, -signal.ValidForBars, signal.EntryPrice, Brushes.SteelBlue, DashStyleHelper.Solid, 2);
            Draw.Line(this, tagPrefix + ".Stop", false, 0, signal.StopPrice, -signal.ValidForBars, signal.StopPrice, Brushes.Firebrick, DashStyleHelper.Dash, 2);
            Draw.Line(this, tagPrefix + ".Target", false, 0, signal.TargetPrice, -signal.ValidForBars, signal.TargetPrice, Brushes.ForestGreen, DashStyleHelper.Dash, 2);

            string label = string.Format(
                "{0}\nEntrada: {1:N2}\nStop: {2:N2}\nAlvo: {3:N2}\nR:R: {4:N2}\n{5}",
                directionText,
                signal.EntryPrice,
                signal.StopPrice,
                signal.TargetPrice,
                signal.RiskRewardRatio,
                signal.Reason);

            double textPrice = isLong
                ? markerPrice - (TickSize * 6)
                : markerPrice + (TickSize * 6);
            Draw.Text(this, tagPrefix + ".Label", label, 0, textPrice, directionBrush);
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
        [Range(1, 20)]
        [Display(Name = "Validade em candles", GroupName = "Sinais", Order = 3)]
        public int ValidForBars { get; set; }

        #endregion
    }
}
