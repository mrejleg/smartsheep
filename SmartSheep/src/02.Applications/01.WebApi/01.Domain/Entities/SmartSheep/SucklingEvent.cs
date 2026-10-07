using Project.Base.Models.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Api.Domain.Entities.MasterDatas;

namespace Api.Domain.Entities.SmartSheep
{
    // Satu kejadian hasil inferensi video (InferenceYolo): menyusu, pendekatan gagal, atau pendekatan.
    // Id dikirim dari worker sehingga sync ulang tidak menggandakan data.
    public class SucklingEvent : BaseEntity<Guid>
    {
        [ForeignKey("FkSucklingActivityId")]
        public SucklingActivity? SucklingActivity { get; set; }
        public Guid? FkSucklingActivityId { get; set; }

        [ForeignKey("FkBarnId")]
        public Barn? Barn { get; set; }
        public Guid? FkBarnId { get; set; }

        [ForeignKey("FkSourceVideoId")]
        public SourceVideo? SourceVideo { get; set; }
        public Guid? FkSourceVideoId { get; set; }

        [Required]
        [Column(TypeName = "VARCHAR(20)")]
        [StringLength(20, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        // Suckling: menyusu > durasi minimal; FailedAttempt: pendekatan gagal; Approach: pendekatan.
        public string EventType { get; set; } = "Suckling";

        public int LambTrackId { get; set; }

        public int EweTrackId { get; set; }

        public DateTime? EventStart { get; set; }

        public DateTime? EventEnd { get; set; }

        public double DurationSeconds { get; set; }

        public int FrameCount { get; set; }
    }
}
