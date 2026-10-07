using Project.Base.Models.Entities;

namespace Web.Domain.Models.ApiManagements
{
    public class ScopeClient : BaseEntity<Guid>
    {
        public string? Name { get; set; }
    }
}
