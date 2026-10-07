using Project.Base.Models.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Api.Domain.Entities.Configs;

namespace Api.Domain.Entities.SmartSheep
{
    public class ReminderLog : BaseEntity<Guid>
    {
        [ForeignKey("FkReminderTemplateId")]
        public ReminderTemplate? ReminderTemplate { get; set; }
        public Guid? FkReminderTemplateId { get; set; }

        [ForeignKey("FkSucklingStatisticId")]
        public SucklingStatistic? SucklingStatistic { get; set; }
        public Guid? FkSucklingStatisticId { get; set; }

        [Required]
        [Column(TypeName = "VARCHAR(100)")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "VARCHAR(20)")]
        [StringLength(20, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        // Pending: waiting to be sent; Sent: successfully sent; Failed: delivery failed.
        public string Status { get; set; } = "Pending";

        public DateTime? ScheduledAt { get; set; }

        public DateTime? SentAt { get; set; }

        [Column(TypeName = "VARCHAR(1000)")]
        [StringLength(1000, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string? ErrorMessage { get; set; }
    }
}
