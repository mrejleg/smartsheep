using Project.Base.Models.Entities;

namespace Web.Domain.Models.Configs
{
    public class AppRole : BaseEntity<Guid>
    {
        public string Name { get; set; } = string.Empty;
    }
}
