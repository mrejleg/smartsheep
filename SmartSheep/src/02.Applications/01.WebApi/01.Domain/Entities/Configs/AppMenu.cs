using Project.Base.Models.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Domain.Entities.Configs
{
    public class AppMenu : BaseEntity<Guid>
    {
        [ForeignKey("FkParentId")]
        public AppMenu? AppMenuParent { get; set; }
        public Guid? FkParentId { get; set; }

        [Required]
        [Column(TypeName = "VARCHAR(256)")]
        [StringLength(256, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string Name { get; set; }

        [Column(TypeName = "VARCHAR(256)")]
        [StringLength(256, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string? Controller { get; set; }

        [Required]
        [Column(TypeName = "VARCHAR(256)")]
        [StringLength(256, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string SequenceNumber { get; set; }

        public string? Icon { get; set; }

        public string? IconActive { get; set; }

        public bool IsSection { get; set; }

        public bool IsMobile { get; set; }

        [Required]
        [Column(TypeName = "VARCHAR(2000)")]
        [StringLength(2000, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string AccessTypes { get; set; }
    }
}
