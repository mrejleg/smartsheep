namespace Api.Domain.Models.Dashboards
{
    public class DashboardHourlyTrendDto
    {
        public int Hour { get; set; }
        public int Frequency { get; set; }
        public int DurationSeconds { get; set; }
    }
}
