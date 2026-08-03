using System;

namespace NinjaTrader.NinjaScript.TradeAssistant.MarketData
{
    public sealed class MarketDeltaSnapshot
    {
        public DateTime BarTime { get; private set; }

        public long BuyVolume { get; private set; }

        public long SellVolume { get; private set; }

        public long UnknownVolume { get; private set; }

        public long Delta
        {
            get { return BuyVolume - SellVolume; }
        }

        public long ClassifiedVolume
        {
            get { return BuyVolume + SellVolume; }
        }

        public long TotalVolume
        {
            get
            {
                return BuyVolume
                    + SellVolume
                    + UnknownVolume;
            }
        }

        public double DeltaPercentage
        {
            get
            {
                if (ClassifiedVolume <= 0)
                    return 0;

                return (double)Delta
                    / ClassifiedVolume
                    * 100.0;
            }
        }

        public MarketDeltaSnapshot(
            DateTime barTime,
            long buyVolume,
            long sellVolume,
            long unknownVolume)
        {
            BarTime = barTime;
            BuyVolume = buyVolume;
            SellVolume = sellVolume;
            UnknownVolume = unknownVolume;
        }
    }
}