using System.ComponentModel.DataAnnotations;

namespace Api.Domain.Models.MobileApps
{
    public class MenuFavoriteParamDto
    {
        public string? Email { get; set; }

        [MaxLength(4, ErrorMessage = "A maximum of four favorite menus can be selected.")]
        public List<Guid?> FkAppMenuIds { get; set; }
    }
}
