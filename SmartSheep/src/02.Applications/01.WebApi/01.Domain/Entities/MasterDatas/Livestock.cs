using Project.Base.Models.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Domain.Entities.MasterDatas
{
    public class Livestock : BaseEntity<Guid>
    {
        [Required]
        [Column(TypeName = "VARCHAR(50)")]
        [StringLength(50, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "VARCHAR(150)")]
        [StringLength(150, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "VARCHAR(10)")]
        [StringLength(10, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        // Male: male livestock; Female: female livestock.
        public string Sex { get; set; } = "Female";
    }
}
