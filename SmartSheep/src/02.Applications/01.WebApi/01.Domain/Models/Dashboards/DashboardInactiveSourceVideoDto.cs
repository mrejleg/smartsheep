namespace Api.Domain.Models.Dashboards
{
    public class DashboardInactiveSourceVideoDto
    {
        public Guid Id { get; set; }
        public string? Code { get; set; }
        public string? BarnName { get; set; }
        public string? SourceVideoUrl { get; set; }
        public string? LastStatus { get; set; }
        public DateTime? LastCheckedAt { get; set; }
    }
}
