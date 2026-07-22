namespace NinjaTrader.NinjaScript.TradeAssistant.Models
{
    public sealed class ValidationStatistics
    {
        public int Total { get; internal set; }
        public int Active { get; internal set; }
        public int TargetHits { get; internal set; }
        public int StopHits { get; internal set; }
        public int Expired { get; internal set; }
        public int Ambiguous { get; internal set; }
        public int RiskRejected { get; internal set; }
        public int MaximumConsecutiveLosses { get; internal set; }
        public double ResultR { get; internal set; }
        public double ResultCurrency { get; internal set; }
        public double AverageWinnerRiskCurrency { get; internal set; }
        public double AverageLoserRiskCurrency { get; internal set; }
        public double MaximumDrawdownR { get; internal set; }
        public double MaximumDrawdownCurrency { get; internal set; }

        public int Decided
        {
            get { return TargetHits + StopHits; }
        }

        public double WinRate
        {
            get { return Decided == 0 ? 0 : (double)TargetHits / Decided * 100.0; }
        }
    }
}
