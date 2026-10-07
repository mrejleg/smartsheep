using Project.Base.Models.Entities;
using Web.Domain.Models.Configs;

namespace Web.Domain.Models.Auths
{
    public class UserLoginRole : BaseEntity<Guid>
    {
        public UserLogin? UserLogin { get; set; }
        public Guid FkUserId { get; set; }

        public AppRole? AppRole { get; set; }
        public Guid FkRoleId { get; set; }
    }
}
