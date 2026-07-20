namespace NinjaTrader.NinjaScript.TradeAssistant.Models
{
    public sealed class SignalStatistics
    {
        public int Total { get; internal set; }
        public int Active { get; internal set; }
        public int TargetHits { get; internal set; }
        public int StopHits { get; internal set; }
        public int Expired { get; internal set; }
        public int Ambiguous { get; internal set; }
        public double TotalR { get; internal set; }

        public int Decided
        {
            get { return TargetHits + StopHits; }
        }

        public double WinRate
        {
            get { return Decided == 0 ? 0 : (double)TargetHits / Decided * 100.0; }
        }

        public double AverageR
        {
            get
            {
                int completed = TargetHits + StopHits + Expired;
                return completed == 0 ? 0 : TotalR / completed;
            }
        }
    }
}
