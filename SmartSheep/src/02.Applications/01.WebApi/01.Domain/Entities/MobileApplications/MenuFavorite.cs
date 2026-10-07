using Api.Domain.Entities.Configs;
using Project.Base.Models.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Domain.Entities.MobileApplications
{
    public class MenuFavorite : BaseEntity<Guid>
    {
        [Required]
        [Column(TypeName = "VARCHAR(256)")]
        [StringLength(256, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string Email { get; set; }

        [ForeignKey("FkAppMenuId")]
        public AppMenu? AppMenu { get; set; }
        public Guid? FkAppMenuId { get; set; }
    }
}
