using Project.Base.Models.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Domain.Entities.SmartSheep
{
    public class SucklingStatistic : BaseEntity<Guid>
    {
        public Guid? FkBarnId { get; set; }
        [ForeignKey(nameof(FkBarnId))]
        [System.Text.Json.Serialization.JsonIgnore]
        public Api.Domain.Entities.MasterDatas.Barn? Barn { get; set; }

        [Required]
        [Column(TypeName = "VARCHAR(50)")]
        [StringLength(50, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "VARCHAR(250)")]
        [StringLength(250, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string PeriodDescription { get; set; } = string.Empty;

        public int TotalFrequency { get; set; }

        public int TotalDurationSeconds { get; set; }

        public bool RequiresReminder { get; set; }

        public DateTime? ActivityFrom { get; set; }

        public DateTime? ActivityTo { get; set; }

        public int HourNumber { get; set; }

        public int TotalApproach { get; set; }

        public int TotalFailedAttempt { get; set; }

        public int LongestGapSeconds { get; set; }

        [Column(TypeName = "VARCHAR(20)")]
        [StringLength(20, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        // Siang (06.00-17.59) atau Malam (18.00-05.59)
        public string? TimeCategory { get; set; }

        public int ObservationSeconds { get; set; }
    }
}
