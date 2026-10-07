using Project.Base.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace Web.Domain.Models.MasterDatas
{
    public class SourceVideo : BaseEntity<Guid>
    {
        public Barn? Barn { get; set; }
        public Guid? FkBarnId { get; set; }

        [Required, StringLength(50, MinimumLength = 1)]
        public string Code { get; set; } = string.Empty;

        [Required, StringLength(1000, MinimumLength = 1)]
        public string SourceVideoUrl { get; set; } = string.Empty;

        // True: stored video can be accessed; False: stored video cannot be accessed.
        public bool IsOnline { get; set; }

        [StringLength(100, MinimumLength = 1)]
        public string? Username { get; set; }

        [StringLength(250, MinimumLength = 1)]
        public string? Password { get; set; }
    }
}
