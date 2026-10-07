using Project.Base.Models.Entities;
using System.ComponentModel.DataAnnotations.Schema;
using Api.Domain.Entities.MasterDatas;

namespace Api.Domain.Entities.SmartSheep
{
    public class SucklingActivity : BaseEntity<Guid>
    {
        [ForeignKey("FkBarnId")]
        public Barn? Barn { get; set; }
        public Guid? FkBarnId { get; set; }

        public DateTime? ActivityStart { get; set; }

        public DateTime? ActivityEnd { get; set; }

        public int TotalFrequency { get; set; }

        public int TotalDurationSeconds { get; set; }
    }
}
