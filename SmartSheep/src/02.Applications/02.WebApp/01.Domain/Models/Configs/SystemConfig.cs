using Project.Base.Models.Entities;

namespace Web.Domain.Models.Configs
{
    public class SystemConfig : BaseEntity<Guid>
    {
        public int StatisticsDurationMinutes { get; set; } = 120;

        public int SourceVideoHealthCheckIntervalMinutes { get; set; } = 5;
    }
}
