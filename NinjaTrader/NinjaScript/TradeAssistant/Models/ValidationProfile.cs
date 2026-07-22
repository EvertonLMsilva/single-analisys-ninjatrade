namespace NinjaTrader.NinjaScript.TradeAssistant.Models
{
    public sealed class ValidationProfile
    {
        public ValidationProfile(
            SignalSetup setup,
            ValidationStage stage,
            double targetR,
            bool renderOnChart)
        {
            Setup = setup;
            Stage = stage;
            TargetR = targetR;
            RenderOnChart = renderOnChart;
        }

        public SignalSetup Setup { get; private set; }
        public ValidationStage Stage { get; private set; }
        public double TargetR { get; private set; }
        public bool RenderOnChart { get; private set; }
    }
}
