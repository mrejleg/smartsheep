using Project.Base.Models.Entities;
using System.ComponentModel.DataAnnotations;
using Web.Domain.Models.Configs;

namespace Web.Domain.Models.SmartSheep
{
    public class ReminderLog : BaseEntity<Guid>
    {
        public ReminderTemplate? ReminderTemplate { get; set; }
        public Guid? FkReminderTemplateId { get; set; }

        public SucklingStatistic? SucklingStatistic { get; set; }
        public Guid? FkSucklingStatisticId { get; set; }

        [Required, StringLength(100, MinimumLength = 1)]
        public string Username { get; set; } = string.Empty;

        [Required, StringLength(20, MinimumLength = 1)]
        // Pending: waiting to be sent; Sent: successfully sent; Failed: delivery failed.
        public string Status { get; set; } = "Pending";

        public DateTime? ScheduledAt { get; set; }

        public DateTime? SentAt { get; set; }

        [StringLength(1000, MinimumLength = 1)]
        public string? ErrorMessage { get; set; }
    }
}
