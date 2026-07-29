using System;

namespace NinjaTrader.NinjaScript.TradeAssistant.Configuration
{
    public static class IntradayMomentumPlan
    {
        public const string RoundId = "intraday-momentum-2026-07-v1";
        public const int OpeningWindowMinutes = 30;
        public const int OpeningVolatilityLookbackSessions = 20;
        public const int MinimumProspectiveSessions = 20;
        public const int MinimumProspectiveTrades = 12;
        public const double RiskCurrencyPerMicro = 75.0;
        public const double RoundTurnCostCurrency = 5.0;
        public const double MinimumProspectiveProfitFactor = 1.20;
        public const double MaximumProspectiveDrawdownCurrency = 500.0;
        public const int MaximumInactiveSessionGap = 5;
        public static readonly DateTime ProspectiveStartDate =
            new DateTime(2026, 7, 30);

        public static bool IsEligibleInstrument(string instrument)
        {
            if (string.IsNullOrWhiteSpace(instrument))
                return false;

            string trimmed = instrument.Trim();
            int separator = trimmed.IndexOf(' ');
            string master = separator < 0
                ? trimmed
                : trimmed.Substring(0, separator);
            return master.Equals("MNQ", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsProspective(DateTime time)
        {
            return time.Date >= ProspectiveStartDate.Date;
        }

        public static string GetSamplePhase(DateTime time)
        {
            return IsProspective(time)
                ? "Prospective"
                : "HistoricalDevelopment";
        }

        public static bool IsUsDaylightSaving(DateTime day)
        {
            DateTime marchFirst = new DateTime(day.Year, 3, 1);
            int daysToSunday = ((int)DayOfWeek.Sunday - (int)marchFirst.DayOfWeek + 7) % 7;
            DateTime secondSundayMarch = marchFirst.AddDays(daysToSunday + 7);
            DateTime novemberFirst = new DateTime(day.Year, 11, 1);
            daysToSunday = ((int)DayOfWeek.Sunday - (int)novemberFirst.DayOfWeek + 7) % 7;
            DateTime firstSundayNovember = novemberFirst.AddDays(daysToSunday);
            return day.Date >= secondSundayMarch.Date
                && day.Date < firstSundayNovember.Date;
        }

        public static DateTime GetRegularOpen(DateTime tradingDay)
        {
            int hour = IsUsDaylightSaving(tradingDay) ? 10 : 11;
            return tradingDay.Date.AddHours(hour).AddMinutes(30);
        }

        public static DateTime GetRegularClose(DateTime tradingDay)
        {
            return GetRegularOpen(tradingDay).AddHours(6).AddMinutes(30);
        }
    }
}
