using Project.Base.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace Web.Domain.Models.SmartSheep
{
    public class SucklingStatistic : BaseEntity<Guid>
    {
        public Guid? FkBarnId { get; set; }
        [Required, StringLength(50, MinimumLength = 1)]
        public string Code { get; set; } = string.Empty;

        [Required, StringLength(250, MinimumLength = 1)]
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

        [StringLength(20, MinimumLength = 1)]
        public string? TimeCategory { get; set; }

        public int ObservationSeconds { get; set; }
    }
}
