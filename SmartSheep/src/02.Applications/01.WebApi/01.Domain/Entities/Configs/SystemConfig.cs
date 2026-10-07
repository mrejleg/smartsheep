using Project.Base.Models.Entities;

namespace Api.Domain.Entities.Configs
{
    public class SystemConfig : BaseEntity<Guid>
    {
        public int StatisticsDurationMinutes { get; set; } = 120;

        public int SourceVideoHealthCheckIntervalMinutes { get; set; } = 5;
    }
}
