using Project.Base.Models.Entities;
using Web.Domain.Models.MasterDatas;

namespace Web.Domain.Models.SmartSheep
{
    public class SucklingActivity : BaseEntity<Guid>
    {
        public Barn? Barn { get; set; }
        public Guid? FkBarnId { get; set; }

        public DateTime? ActivityStart { get; set; }

        public DateTime? ActivityEnd { get; set; }

        public int TotalFrequency { get; set; }

        public int TotalDurationSeconds { get; set; }
    }
}
