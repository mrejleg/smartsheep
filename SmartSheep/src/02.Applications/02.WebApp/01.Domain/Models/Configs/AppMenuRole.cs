using Project.Base.Models.Entities;

namespace Web.Domain.Models.Configs
{
    public class AppMenuRole : BaseEntity<Guid>
    {
        public AppRole? AppRole { get; set; }
        public Guid? FkAppRoleId { get; set; }

        public AppMenu? AppMenu { get; set; }
        public Guid? FkAppMenuId { get; set; }

        public string AccessTypes { get; set; } = string.Empty;
    }
}
