namespace Api.Domain.Models.Dashboards
{
    public class DashboardFilterParam
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public Guid? FarmId { get; set; }
        public Guid? BarnId { get; set; }
    }
}
