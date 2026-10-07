using Project.Base.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace Web.Domain.Models.MasterDatas
{
    public class Barn : BaseEntity<Guid>
    {
        [Required, StringLength(50, MinimumLength = 1)]
        public string Code { get; set; } = string.Empty;

        [Required, StringLength(150, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        public int Capacity { get; set; }

        public double? Length { get; set; }

        public double? Width { get; set; }

        [Required, StringLength(20, MinimumLength = 1)]
        // Small: small-capacity barn; Large: large-capacity barn.
        public string Type { get; set; } = "Small";

        public Farm? Farm { get; set; }
        public Guid? FkFarmId { get; set; }

        public Livestock? Livestock { get; set; }
        public Guid? FkLivestockId { get; set; }
    }
}
