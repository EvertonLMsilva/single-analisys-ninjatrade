namespace NinjaTrader.NinjaScript.TradeAssistant.Models
{
    public sealed class SignalContext
    {
        public static readonly SignalContext Empty = new SignalContext(
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, false, "Sem contexto");

        public SignalContext(
            double sessionVwap,
            double vwapSlopeAtr,
            double vwapDistanceAtr,
            double fastEmaSlopeAtr,
            double slowEmaSlopeAtr,
            double sessionHigh,
            double sessionLow,
            double candleBodyAtr,
            double closeLocation,
            double relativeVolume,
            int score,
            bool passed,
            string summary)
        {
            SessionVwap = sessionVwap;
            VwapSlopeAtr = vwapSlopeAtr;
            VwapDistanceAtr = vwapDistanceAtr;
            FastEmaSlopeAtr = fastEmaSlopeAtr;
            SlowEmaSlopeAtr = slowEmaSlopeAtr;
            SessionHigh = sessionHigh;
            SessionLow = sessionLow;
            CandleBodyAtr = candleBodyAtr;
            CloseLocation = closeLocation;
            RelativeVolume = relativeVolume;
            Score = score;
            Passed = passed;
            Summary = summary;
        }

        public double SessionVwap { get; private set; }
        public double VwapSlopeAtr { get; private set; }
        public double VwapDistanceAtr { get; private set; }
        public double FastEmaSlopeAtr { get; private set; }
        public double SlowEmaSlopeAtr { get; private set; }
        public double SessionHigh { get; private set; }
        public double SessionLow { get; private set; }
        public double CandleBodyAtr { get; private set; }
        public double CloseLocation { get; private set; }
        public double RelativeVolume { get; private set; }
        public int Score { get; private set; }
        public bool Passed { get; private set; }
        public string Summary { get; private set; }

        public SignalContext WithDecision(bool passed, string decision)
        {
            string baseSummary = Summary
                .Replace(" | APROVADO", string.Empty)
                .Replace(" | REPROVADO", string.Empty);
            return new SignalContext(
                SessionVwap,
                VwapSlopeAtr,
                VwapDistanceAtr,
                FastEmaSlopeAtr,
                SlowEmaSlopeAtr,
                SessionHigh,
                SessionLow,
                CandleBodyAtr,
                CloseLocation,
                RelativeVolume,
                Score,
                passed,
                baseSummary + " | " + decision);
        }
    }
}
