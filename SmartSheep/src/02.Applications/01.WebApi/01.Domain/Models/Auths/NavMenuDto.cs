using Api.Domain.Entities.Configs;

namespace Api.Domain.Models.Auths
{
    public class NavMenuDto
    {
        public List<AppMenu> MenuParents { get; set; }
        public List<AppMenu> MenuChilds { get; set; }
        public List<AppMenuRole> MenuRoles { get; set; }
    }
}
