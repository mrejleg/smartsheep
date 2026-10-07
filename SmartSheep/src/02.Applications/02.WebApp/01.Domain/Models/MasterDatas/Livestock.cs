using Project.Base.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace Web.Domain.Models.MasterDatas
{
    public class Livestock : BaseEntity<Guid>
    {
        [Required, StringLength(50, MinimumLength = 1)]
        public string Code { get; set; } = string.Empty;

        [Required, StringLength(150, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(10, MinimumLength = 1)]
        // Male: male livestock; Female: female livestock.
        public string Sex { get; set; } = "Female";
    }
}
