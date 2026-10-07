using Project.Base.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace Web.Domain.Models.Configs
{
    public class ReminderTemplate : BaseEntity<Guid>
    {
        [Required, StringLength(50, MinimumLength = 1)]
        public string Code { get; set; } = string.Empty;

        [Required, StringLength(150, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(2000, MinimumLength = 1)]
        public string ReminderText { get; set; } = string.Empty;
    }
}
