namespace Project.Core.CronJobs
{
    public static class CronCommands
    {
        public static string CronSecondly()
        {
            return "* * * * * *";
        }

        public static string CronSecondInterval(int interval)
        {
            return interval.ToString() + " * * * * *";
        }

        public static string CronMinutely()
        {
            return "* * * * *";
        }

        public static string CronHourly()
        {
            return CronHourly(0);
        }

        public static string CronHourly(int minute)
        {
            return $"{minute} * * * *";
        }

        public static string CronDaily()
        {
            return CronDaily(0);
        }

        public static string CronDaily(int hour)
        {
            return CronDaily(hour, 0);
        }

        public static string CronDaily(int hour, int minute)
        {
            return $"{minute} {hour} * * *";
        }

        public static string CronWeekly()
        {
            return CronWeekly(DayOfWeek.Monday);
        }

        // Summary:
        //     Returns cron expression that fires every week at 00:00 UTC of the specified day
        //     of the week.
        //
        // Parameters:
        //   dayOfWeek:
        //     The day of week in which the schedule will be activated.
        public static string CronWeekly(DayOfWeek dayOfWeek)
        {
            return CronWeekly(dayOfWeek, 0);
        }

        // Summary:
        //     Returns cron expression that fires every week at the first minute of the specified
        //     day of week and hour in UTC.
        //
        // Parameters:
        //   dayOfWeek:
        //     The day of week in which the schedule will be activated.
        //
        //   hour:
        //     The hour in which the schedule will be activated (0-23).
        public static string CronWeekly(DayOfWeek dayOfWeek, int hour)
        {
            return CronWeekly(dayOfWeek, hour, 0);
        }

        //
        // Summary:
        //     Returns cron expression that fires every week at the specified day of week, hour
        //     and minute in UTC.
        //
        // Parameters:
        //   dayOfWeek:
        //     The day of week in which the schedule will be activated.
        //
        //   hour:
        //     The hour in which the schedule will be activated (0-23).
        //
        //   minute:
        //     The minute in which the schedule will be activated (0-59).
        public static string CronWeekly(DayOfWeek dayOfWeek, int hour, int minute)
        {
            return $"{minute} {hour} * * {(int)dayOfWeek}";
        }

        // Summary:
        //     Returns cron expression that fires every month at 00:00 UTC of the first day
        //     of month.
        public static string CronMonthly()
        {
            return CronMonthly(1);
        }

        // Summary:
        //     Returns cron expression that fires every month at 00:00 UTC of the specified
        //     day of month.
        //
        // Parameters:
        //   day:
        //     The day of month in which the schedule will be activated (1-31).
        public static string CronMonthly(int day)
        {
            return CronMonthly(day, 0);
        }

        // Summary:
        //     Returns cron expression that fires every month at the first minute of the specified
        //     day of month and hour in UTC.
        //
        // Parameters:
        //   day:
        //     The day of month in which the schedule will be activated (1-31).
        //
        //   hour:
        //     The hour in which the schedule will be activated (0-23).
        public static string CronMonthly(int day, int hour)
        {
            return CronMonthly(day, hour, 0);
        }

        // Summary:
        //     Returns cron expression that fires every month at the specified day of month,
        //     hour and minute in UTC.
        //
        // Parameters:
        //   day:
        //     The day of month in which the schedule will be activated (1-31).
        //
        //   hour:
        //     The hour in which the schedule will be activated (0-23).
        //
        //   minute:
        //     The minute in which the schedule will be activated (0-59).
        public static string CronMonthly(int day, int hour, int minute)
        {
            return $"{minute} {hour} {day} * *";
        }

        // Summary:
        //     Returns cron expression that fires every year on Jan, 1st at 00:00 UTC.
        public static string CronYearly()
        {
            return CronYearly(1);
        }

        // Summary:
        //     Returns cron expression that fires every year in the first day at 00:00 UTC of
        //     the specified month.
        //
        // Parameters:
        //   month:
        //     The month in which the schedule will be activated (1-12).
        public static string CronYearly(int month)
        {
            return CronYearly(month, 1);
        }

        // Summary:
        //     Returns cron expression that fires every year at 00:00 UTC of the specified month
        //     and day of month.
        //
        // Parameters:
        //   month:
        //     The month in which the schedule will be activated (1-12).
        //
        //   day:
        //     The day of month in which the schedule will be activated (1-31).
        public static string CronYearly(int month, int day)
        {
            return CronYearly(month, day, 0);
        }

        // Summary:
        //     Returns cron expression that fires every year at the first minute of the specified
        //     month, day and hour in UTC.
        //
        // Parameters:
        //   month:
        //     The month in which the schedule will be activated (1-12).
        //
        //   day:
        //     The day of month in which the schedule will be activated (1-31).
        //
        //   hour:
        //     The hour in which the schedule will be activated (0-23).
        public static string CronYearly(int month, int day, int hour)
        {
            return CronYearly(month, day, hour, 0);
        }

        // Summary:
        //     Returns cron expression that fires every year at the specified month, day, hour
        //     and minute in UTC.
        //
        // Parameters:
        //   month:
        //     The month in which the schedule will be activated (1-12).
        //
        //   day:
        //     The day of month in which the schedule will be activated (1-31).
        //
        //   hour:
        //     The hour in which the schedule will be activated (0-23).
        //
        //   minute:
        //     The minute in which the schedule will be activated (0-59).
        public static string CronYearly(int month, int day, int hour, int minute)
        {
            return $"{minute} {hour} {day} {month} *";
        }

        public static string CronNever()
        {
            return CronYearly(2, 31);
        }
    }
}
