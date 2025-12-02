using System;
using System.Globalization;

namespace Genies.Utilities.Internal
{
    /// <summary>
    ///  Responsible to format all the data into a users visibility, such as time and amount of things
    /// </summary>
    public static class FormattingUtils
    {
        private const decimal _thousand = 1000;
        private const decimal _million = 1000000;
        private const decimal _billion = 1000000000;

        public static DateTimeOffset ConvertDecimalEpochToDate(decimal? value)
        {
            return value.HasValue ? DateTimeOffset.FromUnixTimeSeconds((long)value.Value): DateTimeOffset.Now;
        }

        public static decimal? ConvertDateToDecimalEpoch(DateTime dateTime)
        {
            TimeSpan currentTimeEpoch = dateTime - new DateTime(1970, 1, 1);
            var secondsSinceEpoch = (int)currentTimeEpoch.TotalSeconds;
            decimal? decimalEpoch = secondsSinceEpoch;

            return decimalEpoch;
        }


        public static string FormatTime(decimal? timeStamp, decimal? dataTime)
        {
            DateTimeOffset dateTimeOffset = ConvertDecimalEpochToDate(timeStamp);
            DateTimeOffset dateTimeOffset2 = ConvertDecimalEpochToDate(dataTime);

            DateTime stamp =  dateTimeOffset.DateTime;
            DateTime data =  dateTimeOffset2.DateTime;

            string result = string.Empty;
            var timeSpan = stamp.Subtract(data);

            if (timeSpan >= TimeSpan.FromDays(365))
            {
                result = timeSpan.Days > (365*2) ?
                    $"about {timeSpan.Days / 365} years ago":
                    "about a year ago";
            }
            else if (timeSpan >= TimeSpan.FromDays(30))
            {
                result = timeSpan.Days >= 60 ?
                    String.Format("about {0} months ago", timeSpan.Days / 30) :
                    "about a month ago";
            }
            else if (timeSpan >= TimeSpan.FromDays(1))
            {
                result = timeSpan.Days > 1 ?
                    String.Format("about {0} days ago", timeSpan.Days) :
                    "yesterday";
            }
            else if (timeSpan >= TimeSpan.FromHours(24))
            {
                result = timeSpan.Days > 1 ?
                    String.Format("about {0} days ago", timeSpan.Days) :
                    "yesterday";
            }
            else if (timeSpan >= TimeSpan.FromMinutes(60))
            {
                result = timeSpan.Hours > 1 ?
                    String.Format("about {0} hours ago", timeSpan.Hours) :
                    "about an hour ago";
            }
            else if (timeSpan >= TimeSpan.FromSeconds(60))
            {
                result = timeSpan.Minutes > 1 ?
                    String.Format("about {0} minutes ago", timeSpan.Minutes) :
                    "about a minute ago";
            }
            else if (timeSpan <= TimeSpan.FromSeconds(60))
            {
                result = string.Format("{0} seconds ago", timeSpan.Seconds);
            }

            return result;
        }

        /// <summary>
        /// Returns a decimal as a string formatted as 0-999, 1.0K - 999.9K, 1.00M - 999.99M, or 1.000B+
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        public static string GetFormattedString(decimal num)
        {
            if (num >= _billion || num <= -_billion)
            {
                return num.ToString("0,,,B", CultureInfo.InvariantCulture);
            }
            else if (num >= _million || num <= -_million)
            {
                return num.ToString("0,,M", CultureInfo.InvariantCulture);
            }
            else if (num >= _thousand || num <= -_thousand)
            {
                return num.ToString("0,.#K", CultureInfo.InvariantCulture);
            }
            else
            {
                return num.ToString(CultureInfo.InvariantCulture);
            }
        }
    }
}
