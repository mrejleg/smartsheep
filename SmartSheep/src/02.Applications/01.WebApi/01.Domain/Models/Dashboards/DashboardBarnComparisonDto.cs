namespace Api.Domain.Models.Dashboards
{
    public class DashboardBarnComparisonDto
    {
        public Guid? BarnId { get; set; }
        public string? BarnCode { get; set; }
        public string? BarnName { get; set; }
        public int Frequency { get; set; }
        public int DurationSeconds { get; set; }
    }
}
