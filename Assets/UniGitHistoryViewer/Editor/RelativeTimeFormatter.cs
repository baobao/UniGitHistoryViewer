using System;
using System.Globalization;

namespace info.shibuya24
{
    /// <summary>
    /// Date strings shown in the viewer.
    /// </summary>
    internal static class RelativeTimeFormatter
    {
        private const string AbsoluteFormat = "yyyy-MM-dd HH:mm";

        public static string FormatAbsolute(DateTimeOffset date)
        {
            return date.ToLocalTime().ToString(AbsoluteFormat, CultureInfo.InvariantCulture);
        }

        public static string FormatRelative(DateTimeOffset date, DateTimeOffset now)
        {
            TimeSpan elapsed = now - date;
            if (elapsed.TotalMinutes < 1)
            {
                return "just now";
            }

            if (elapsed.TotalHours < 1)
            {
                return _Plural((int) elapsed.TotalMinutes, "minute");
            }

            if (elapsed.TotalDays < 1)
            {
                return _Plural((int) elapsed.TotalHours, "hour");
            }

            if (elapsed.TotalDays < 30)
            {
                return _Plural((int) elapsed.TotalDays, "day");
            }

            if (elapsed.TotalDays < 365)
            {
                return _Plural((int) (elapsed.TotalDays / 30), "month");
            }

            return _Plural((int) (elapsed.TotalDays / 365), "year");

            string _Plural(int count, string unit)
            {
                return count == 1 ? $"1 {unit} ago" : $"{count} {unit}s ago";
            }
        }
    }
}
