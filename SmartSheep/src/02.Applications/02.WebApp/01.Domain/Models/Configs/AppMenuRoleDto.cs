namespace Web.Domain.Models.Configs
{
    public class AppMenuRoleDto : AppMenuRole
    {
        public List<AppMenu>? Menus { get; set; }
        public List<AppMenuRole>? MenuRoles { get; set; }
    }
}
