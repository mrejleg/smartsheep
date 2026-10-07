namespace Project.Base.Commons.Formats
{
    public static class DateTimeFormats
    {
        public const string dd_MM_yyyy = "dd-MM-yyyy";
        public const string dd_MMM_yyyy = "dd-MMM-yyyy";
        public const string dd_MMMM_yyyy = "dd MMMM yyyy";

        public const string dd_MM_yyyy_HH_mm_ss = "dd-MM-yyyy HH:mm:ss";
        public const string dd_MMM_yyyy_HH_mm_ss = "dd-MMM-yyyy HH:mm:ss";
        public const string dd_MMMM_yyyy_HH_mm_ss = "dd MMMM yyyy HH:mm:ss";

        public const string dd_MMMM_yyyy_HH_mm_ss_zzz = "dd MMMM yyyy HH:mm:ss \"UTC\"zzz";

        public const string yyyy_MM_dd = "yyyy-MM-dd";

        public static string GetMonthDescrInEnglish(int? month)
        {
            switch (month)
            {
                case 1: return "January";
                case 2: return "February";
                case 3: return "March";
                case 4: return "April";
                case 5: return "May";
                case 6: return "June";
                case 7: return "July";
                case 8: return "August";
                case 9: return "September";
                case 10: return "October";
                case 11: return "November";
                case 12: return "December";
                default: return String.Empty;
            }
        }

        public static string GetMonthDescrInBahasa(int? month)
        {
            switch (month)
            {
                case 1: return "Januari";
                case 2: return "Febuari";
                case 3: return "Maret";
                case 4: return "April";
                case 5: return "Mei";
                case 6: return "Juni";
                case 7: return "Juli";
                case 8: return "Agustus";
                case 9: return "September";
                case 10: return "Oktober";
                case 11: return "November";
                case 12: return "Desember";
                default: return String.Empty;
            }
        }
    }
}
