using Project.Base.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace Api.Domain.Entities.Configs
{
    public class EmailTemplate : BaseEntity<Guid>
    {
        [Required]
        [StringLength(256, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string Name { get; set; }

        [Required]
        [StringLength(256, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string Subject { get; set; }

        [Required]
        public string Body { get; set; }

        public bool IsHtmlTemplate { get; set; }

        public string? EmailCc { get; set; }
    }
}
