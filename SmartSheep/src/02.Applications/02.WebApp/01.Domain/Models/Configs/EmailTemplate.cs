using Project.Base.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace Web.Domain.Models.Configs
{
    public class EmailTemplate : BaseEntity<Guid>
    {
        [Required]
        [StringLength(256, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string Name { get; set; }

        [Required]
        [StringLength(256, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string Subject { get; set; }

        public string Body { get; set; } = string.Empty;

        public bool IsHtmlTemplate { get; set; }
        public string? BodyType { get; set; }

        public string? HtmlBody { get; set; }
        public string? TextBody { get; set; }

        public string? EmailCc { get; set; }
    }
}
