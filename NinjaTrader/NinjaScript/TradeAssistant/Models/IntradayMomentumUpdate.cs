namespace NinjaTrader.NinjaScript.TradeAssistant.Models
{
    public sealed class IntradayMomentumUpdate
    {
        public IntradayMomentumUpdate(
            IntradayMomentumTrade openedTrade,
            IntradayMomentumTrade closedTrade)
        {
            OpenedTrade = openedTrade;
            ClosedTrade = closedTrade;
        }

        public IntradayMomentumTrade OpenedTrade { get; private set; }
        public IntradayMomentumTrade ClosedTrade { get; private set; }

        public bool HasChanges
        {
            get { return OpenedTrade != null || ClosedTrade != null; }
        }

        public static IntradayMomentumUpdate None
        {
            get { return new IntradayMomentumUpdate(null, null); }
        }
    }
}
