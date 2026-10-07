namespace Web.Domain.Models.Dashboards
{
    public class DashboardFarmMap
    {
        public Guid Id { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Location { get; set; }
        public string? Address { get; set; }
        public string? Latitude { get; set; }
        public string? Longitude { get; set; }
        public int TotalBarns { get; set; }
        public int TotalSheep { get; set; }
        public int TotalSourceVideos { get; set; }
        public int OnlineSourceVideos { get; set; }
    }
}
