using Project.Base.Models.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace Web.Domain.Models.Configs
{
    public class NewsImageUploadResult
    {
        public string Path { get; set; } = string.Empty;
    }

    public class News : BaseEntity<Guid>
    {
        [Required, StringLength(50, MinimumLength = 1)]
        public string Code { get; set; } = string.Empty;

        [Required, StringLength(256, MinimumLength = 1)]
        public string Title { get; set; } = string.Empty;

        [Required, StringLength(500, MinimumLength = 1)]
        public string ShortContent { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        [StringLength(1000, MinimumLength = 1)]
        public string? ImageThumbnail { get; set; }

        [NotMapped]
        public IFormFile? ImageFile { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }
    }
}
