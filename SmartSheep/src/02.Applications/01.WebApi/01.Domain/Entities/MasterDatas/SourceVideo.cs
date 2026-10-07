using Project.Base.Models.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Domain.Entities.MasterDatas
{
    public class SourceVideo : BaseEntity<Guid>
    {
        [ForeignKey("FkBarnId")]
        public Barn? Barn { get; set; }
        public Guid? FkBarnId { get; set; }

        [Required]
        [Column(TypeName = "VARCHAR(50)")]
        [StringLength(50, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "VARCHAR(1000)")]
        [StringLength(1000, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string SourceVideoUrl { get; set; } = string.Empty;

        // True: stored video can be accessed; False: stored video cannot be accessed.
        public bool IsOnline { get; set; }

        [Column(TypeName = "VARCHAR(100)")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string? Username { get; set; }

        [Column(TypeName = "VARCHAR(250)")]
        [StringLength(250, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string? Password { get; set; }
    }
}
