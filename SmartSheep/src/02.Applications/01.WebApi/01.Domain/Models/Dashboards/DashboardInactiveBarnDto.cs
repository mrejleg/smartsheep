namespace Api.Domain.Models.Dashboards
{
    public class DashboardInactiveBarnDto
    {
        public Guid Id { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? FarmName { get; set; }
        public int TotalSourceVideos { get; set; }
        public int OnlineSourceVideos { get; set; }
    }
}
