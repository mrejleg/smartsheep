using Project.Base.Models.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Domain.Entities.Configs
{
    public class AppRole : BaseEntity<Guid>
    {
        [Required]
        [Column(TypeName = "VARCHAR(256)")]
        [StringLength(256, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string Name { get; set; }
    }
}
