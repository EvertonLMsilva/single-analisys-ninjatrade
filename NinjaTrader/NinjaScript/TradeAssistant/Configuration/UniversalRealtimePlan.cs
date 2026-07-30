using System;

namespace NinjaTrader.NinjaScript.TradeAssistant.Configuration
{
    public static class UniversalRealtimePlan
    {
        public const string RoundId = "universal-realtime-2026-07-v1";
        public const int DefaultStartTime = 103000;
        public const int DefaultEndTime = 170000;
        public const int RequiredMinutePeriod = 5;

        public static bool IsInsideWindow(
            DateTime time,
            int startTime,
            int endTime)
        {
            if (!IsValidTimeValue(startTime)
                || !IsValidTimeValue(endTime)
                || startTime > endTime)
            {
                return false;
            }

            int currentTime =
                (time.Hour * 10000) + (time.Minute * 100) + time.Second;
            return currentTime >= startTime && currentTime <= endTime;
        }

        public static bool IsValidTimeValue(int value)
        {
            if (value < 0 || value > 235959)
                return false;

            int minutes = (value / 100) % 100;
            int seconds = value % 100;
            return minutes < 60 && seconds < 60;
        }
    }
}
