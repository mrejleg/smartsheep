using Web.Domain.Models.Configs;

namespace Web.Domain.Models.Auths
{
    public class NavMenuDto
    {
        public List<AppMenu> MenuParents { get; set; }
        public List<AppMenu> MenuChilds { get; set; }
        public List<AppMenuRole> MenuRoles { get; set; }
    }
}
