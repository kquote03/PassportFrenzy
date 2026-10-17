using UnityEngine;

namespace PassportFrenzy
{
    public static class TimeFormat
    {
        /// <summary>"5 years (5 min)" in normal mode, "5 years (30 s)" in demo mode. Always whole numbers.</summary>
        public static string Years(int years, float secondsPerYear)
        {
            string y = years == 1 ? "1 year" : years + " years";
            return $"{y} ({RealDuration(years * secondsPerYear)})";
        }

        /// <summary>Whole minutes when at least a minute, else whole seconds.</summary>
        public static string RealDuration(float seconds)
        {
            if (seconds >= 59.5f) return Mathf.RoundToInt(seconds / 60f) + " min";
            return Mathf.RoundToInt(seconds) + " s";
        }

        /// <summary>"1:05" style clock, rounded up so 0:00 means really out of time.</summary>
        public static string Clock(float seconds)
        {
            int s = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return $"{s / 60}:{s % 60:00}";
        }

        /// <summary>Fractional years, one decimal ("2.4").</summary>
        public static string YearsDecimal(float seconds, float secondsPerYear) =>
            (seconds / Mathf.Max(0.01f, secondsPerYear)).ToString("0.0");
    }
}
