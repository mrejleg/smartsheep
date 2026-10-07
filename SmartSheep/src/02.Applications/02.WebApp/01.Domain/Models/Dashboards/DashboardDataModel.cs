namespace Web.Domain.Models.Dashboards
{
    public class DashboardDataModel
    {
        public DateTime Date { get; set; }
        public int TotalFarms { get; set; }
        public int TotalBarns { get; set; }
        public int TotalSheep { get; set; }
        public int TodayActivities { get; set; }
        public int TodayFrequency { get; set; }
        public int TodayDurationSeconds { get; set; }
        public int StatisticsRequiringReminder { get; set; }
        public List<DashboardHourlyTrend> HourlyTrend { get; set; } = new();
        public List<DashboardBarnComparison> BarnComparison { get; set; } = new();
        public List<DashboardReminderStatus> Reminders { get; set; } = new();
        public List<DashboardInactiveSourceVideo> InactiveSourceVideos { get; set; } = new();
        public List<DashboardInactiveBarn> InactiveBarns { get; set; } = new();
        public List<DashboardFarmMap> FarmMaps { get; set; } = new();
    }
}
