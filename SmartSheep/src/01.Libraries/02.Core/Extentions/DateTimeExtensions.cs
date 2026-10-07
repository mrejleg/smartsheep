using Project.Base.Commons.Formats;
using System;

namespace Project.Core.Extentions
{
    public static class DateTimeExtensions
    {
        public static string ToShortDateDisplayText(this DateTime dateTime)
        {
            return dateTime.ToString(DateTimeFormats.dd_MMM_yyyy);
        }

        public static string ToShortDateDisplayText(this DateTimeOffset dateTimeOffest)
        {
            return dateTimeOffest.LocalDateTime.ToString(DateTimeFormats.dd_MMM_yyyy);
        }

        public static string ToShortDateTimeDisplayText(this DateTime dateTime)
        {
            return dateTime.ToString(DateTimeFormats.dd_MMM_yyyy_HH_mm_ss);
        }
        public static string ToShortDateTimeDisplayText(this DateTimeOffset dateTimeOffest)
        {
            return dateTimeOffest.LocalDateTime.ToString(DateTimeFormats.dd_MMM_yyyy_HH_mm_ss);
        }

        public static string ToLongDateDisplayText(this DateTime dateTime)
        {
            return dateTime.ToString(DateTimeFormats.dd_MMMM_yyyy);
        }
        public static string ToLongDateDisplayText(this DateTimeOffset dateTimeOffest)
        {
            return dateTimeOffest.LocalDateTime.ToString(DateTimeFormats.dd_MMMM_yyyy);
        }

        public static string ToLongDateTimeDisplayText(this DateTime dateTime)
        {
            return dateTime.ToString(DateTimeFormats.dd_MMMM_yyyy_HH_mm_ss);
        }
        public static string ToLongDateTimeDisplayText(this DateTimeOffset dateTimeOffest)
        {
            return dateTimeOffest.LocalDateTime.ToString(DateTimeFormats.dd_MMMM_yyyy_HH_mm_ss);
        }

        public static string ToCompleteDateTimeDisplayText(this DateTime dateTime)
        {
            return dateTime.ToString(DateTimeFormats.dd_MMMM_yyyy_HH_mm_ss_zzz);
        }
        public static string ToCompleteDateTimeDisplayText(this DateTimeOffset dateTimeOffest)
        {
            return dateTimeOffest.LocalDateTime.ToString(DateTimeFormats.dd_MMMM_yyyy_HH_mm_ss_zzz);
        }

        public static int GetQuarter(this DateTime dateTime)
        {
            var month = dateTime.Month;
            if (month == 1 || month == 2 || month == 3)
            {
                return 1;
            }
            else if (month == 4 || month == 5 || month == 6)
            {
                return 2;
            }
            else if (month == 7 || month == 8 || month == 9)
            {
                return 3;
            }
            else
            {
                return 4;
            }
        }

        public static int GetQuarter(this DateTimeOffset dateTimeOffest)
        {
            var month = dateTimeOffest.LocalDateTime.Month;
            if (month == 1 || month == 2 || month == 3)
            {
                return 1;
            }
            else if (month == 4 || month == 5 || month == 6)
            {
                return 2;
            }
            else if (month == 7 || month == 8 || month == 9)
            {
                return 3;
            }
            else
            {
                return 4;
            }
        }

        public static int GetSemester(this DateTime dateTime)
        {
            var month = dateTime.Month;
            if (month == 1 || month == 2 || month == 3 || month == 4 || month == 5 || month == 6)
            {
                return 1;
            }
            else
            {
                return 2;
            }
        }

        public static int GetSemester(this DateTimeOffset dateTimeOffest)
        {
            var month = dateTimeOffest.LocalDateTime.Month;
            if (month == 1 || month == 2 || month == 3 || month == 4 || month == 5 || month == 6)
            {
                return 1;
            }
            else
            {
                return 2;
            }
        }

        public static string ToFriendlyTimeDisplayText(this DateTime dateTime)
        {
            var hour = dateTime.Hour;

            if (hour >= 4 && hour < 12)
            {
                return "Morning";
            }
            else if (hour >= 12 && hour < 17)
            {
                return "Afternoon";
            }
            else if (hour >= 17 && hour < 21)
            {
                return "Evening";
            }
            else
            {
                return "Night";
            }
        }

        public static string ToFriendlyTimeDisplayText(this DateTimeOffset dateTimeOffest)
        {
            var hour = dateTimeOffest.Hour;

            if (hour >= 4 && hour < 12)
            {
                return "Morning";
            }
            else if (hour >= 12 && hour < 17)
            {
                return "Afternoon";
            }
            else if (hour >= 17 && hour < 21)
            {
                return "Evening";
            }
            else
            {
                return "Night";
            }
        }

        public static string ToDayNameInEnglish(this DateTime dateTime)
        {
            return dateTime.ToString("dddd");
        }

        public static string ToDayNameInEnglish(this DateTimeOffset dateTimeOffest)
        {
            return dateTimeOffest.ToString("dddd");
        }

        public static string ToDayNameInBahasa(this DateTime dateTime)
        {
            var dayName = dateTime.ToString("dddd");

            string result = string.Empty;

            if (dayName.ToLower() == "monday")
            {
                result = "Senin";
            }
            else if (dayName.ToLower() == "tuesday")
            {
                result = "Selasa";
            }
            else if (dayName.ToLower() == "wednesday")
            {
                result = "Rabu";
            }
            else if (dayName.ToLower() == "thursday")
            {
                result = "Kamis";
            }
            else if (dayName.ToLower() == "friday")
            {
                result = "Jumat";
            }
            else if (dayName.ToLower() == "saturday")
            {
                result = "Sabtu";
            }
            else if (dayName.ToLower() == "sunday")
            {
                result = "Minggu";
            }

            return result;
        }

        public static string ToDayNameInBahasa(this DateTimeOffset dateTimeOffest)
        {
            var dayName = dateTimeOffest.ToString("dddd");

            string result = string.Empty;

            if (dayName.ToLower() == "monday")
            {
                result = "Senin";
            }
            else if (dayName.ToLower() == "tuesday")
            {
                result = "Selasa";
            }
            else if (dayName.ToLower() == "wednesday")
            {
                result = "Rabu";
            }
            else if (dayName.ToLower() == "thursday")
            {
                result = "Kamis";
            }
            else if (dayName.ToLower() == "friday")
            {
                result = "Jumat";
            }
            else if (dayName.ToLower() == "saturday")
            {
                result = "Sabtu";
            }
            else if (dayName.ToLower() == "sunday")
            {
                result = "Minggu";
            }

            return result;
        }

        public static string ToMonthNameInBahasa(this DateTime dateTime)
        {
            string result = string.Empty;

            if (dateTime.Month == 1)
            {
                result = "Jan";
            }
            if (dateTime.Month == 2)
            {
                result = "Feb";
            }
            if (dateTime.Month == 3)
            {
                result = "Mar";
            }
            if (dateTime.Month == 4)
            {
                result = "Apr";
            }
            if (dateTime.Month == 5)
            {
                result = "May";
            }
            if (dateTime.Month == 6)
            {
                result = "Jun";
            }
            if (dateTime.Month == 7)
            {
                result = "Jul";
            }
            if (dateTime.Month == 8)
            {
                result = "Aug";
            }
            if (dateTime.Month == 9)
            {
                result = "Sep";
            }
            if (dateTime.Month == 10)
            {
                result = "Oct";
            }
            if (dateTime.Month == 11)
            {
                result = "Nov";
            }
            if (dateTime.Month == 12)
            {
                result = "Dec";
            }

            return result;
        }

        public static string ToMonthNameInBahasa(this DateTimeOffset dateTimeOffest)
        {
            string result = string.Empty;

            if (dateTimeOffest.Month == 1)
            {
                result = "Jan";
            }
            if (dateTimeOffest.Month == 2)
            {
                result = "Feb";
            }
            if (dateTimeOffest.Month == 3)
            {
                result = "Mar";
            }
            if (dateTimeOffest.Month == 4)
            {
                result = "Apr";
            }
            if (dateTimeOffest.Month == 5)
            {
                result = "May";
            }
            if (dateTimeOffest.Month == 6)
            {
                result = "Jun";
            }
            if (dateTimeOffest.Month == 7)
            {
                result = "Jul";
            }
            if (dateTimeOffest.Month == 8)
            {
                result = "Aug";
            }
            if (dateTimeOffest.Month == 9)
            {
                result = "Sep";
            }
            if (dateTimeOffest.Month == 10)
            {
                result = "Oct";
            }
            if (dateTimeOffest.Month == 11)
            {
                result = "Nov";
            }
            if (dateTimeOffest.Month == 12)
            {
                result = "Dec";
            }

            return result;
        }

        public static string ToMonthNameInBahasa(this int month)
        {
            string result = string.Empty;

            if (month == 1)
            {
                result = "Jan";
            }
            if (month == 2)
            {
                result = "Feb";
            }
            if (month == 3)
            {
                result = "Mar";
            }
            if (month == 4)
            {
                result = "Apr";
            }
            if (month == 5)
            {
                result = "May";
            }
            if (month == 6)
            {
                result = "Jun";
            }
            if (month == 7)
            {
                result = "Jul";
            }
            if (month == 8)
            {
                result = "Aug";
            }
            if (month == 9)
            {
                result = "Sep";
            }
            if (month == 10)
            {
                result = "Oct";
            }
            if (month == 11)
            {
                result = "Nov";
            }
            if (month == 12)
            {
                result = "Dec";
            }

            return result;
        }
    }
}