namespace Api.Domain.Models.Dashboards
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
        public List<DashboardHourlyTrendDto> HourlyTrend { get; set; } = new();
        public List<DashboardBarnComparisonDto> BarnComparison { get; set; } = new();
        public List<DashboardReminderStatusDto> Reminders { get; set; } = new();
        public List<DashboardInactiveSourceVideoDto> InactiveSourceVideos { get; set; } = new();
        public List<DashboardInactiveBarnDto> InactiveBarns { get; set; } = new();
        public List<DashboardFarmMapDto> FarmMaps { get; set; } = new();
    }
}
