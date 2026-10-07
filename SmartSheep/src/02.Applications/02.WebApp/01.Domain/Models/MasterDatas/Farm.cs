using Project.Base.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace Web.Domain.Models.MasterDatas
{
    public class Farm : BaseEntity<Guid>
    {
        [Required, StringLength(50, MinimumLength = 1)]
        public string Code { get; set; } = string.Empty;

        [Required, StringLength(150, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        [StringLength(250, MinimumLength = 1)]
        public string? Location { get; set; }

        [StringLength(50, MinimumLength = 1)]
        public string? Longitude { get; set; }

        [StringLength(50, MinimumLength = 1)]
        public string? Latitude { get; set; }

        [StringLength(500, MinimumLength = 1)]
        public string? Address { get; set; }
    }
}
