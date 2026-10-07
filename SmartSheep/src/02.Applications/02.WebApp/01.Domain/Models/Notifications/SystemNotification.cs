using Project.Base.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace Web.Domain.Models.Notifications
{
    public class SystemNotification : BaseEntity<Guid>
    {
        public string? Code { get; set; }
        public string? Type { get; set; }

        [StringLength(100)]
        public string? Username { get; set; }

        [StringLength(256)]
        public string? PositionId { get; set; }

        [StringLength(500)]
        public string? Title { get; set; }

        public string? Content { get; set; }

        public bool IsRead { get; set; }

        public DateTime? DateRead { get; set; }

        public string? ReturnLink { get; set; }
    }
}
