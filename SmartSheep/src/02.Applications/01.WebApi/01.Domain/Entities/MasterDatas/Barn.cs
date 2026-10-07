using Project.Base.Models.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Domain.Entities.MasterDatas
{
    public class Barn : BaseEntity<Guid>
    {
        [Required]
        [Column(TypeName = "VARCHAR(50)")]
        [StringLength(50, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "VARCHAR(150)")]
        [StringLength(150, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        public int Capacity { get; set; }

        public double? Length { get; set; }

        public double? Width { get; set; }

        [Required]
        [Column(TypeName = "VARCHAR(20)")]
        [StringLength(20, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        // Small: small-capacity barn; Large: large-capacity barn.
        public string Type { get; set; } = "Small";

        [ForeignKey("FkFarmId")]
        public Farm? Farm { get; set; }
        public Guid? FkFarmId { get; set; }

        [ForeignKey("FkLivestockId")]
        public Livestock? Livestock { get; set; }
        public Guid? FkLivestockId { get; set; }
    }
}
