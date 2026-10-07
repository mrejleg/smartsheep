using Project.Base.Models.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Domain.Entities.Configs
{
    public class AppMenuRole : BaseEntity<Guid>
    {
        [ForeignKey("FkAppRoleId")]
        public AppRole? AppRole { get; set; }
        public Guid? FkAppRoleId { get; set; }

        [ForeignKey("FkAppMenuId")]
        public AppMenu? AppMenu { get; set; }
        public Guid? FkAppMenuId { get; set; }

        [Required]
        [Column(TypeName = "VARCHAR(2000)")]
        [StringLength(2000, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string AccessTypes { get; set; }
    }
}
