using Api.Domain.Entities.Configs;
using Project.Base.Models.Entities;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Domain.Entities.Auths;

public class UserLoginRole : BaseEntity<Guid>
{
    [ForeignKey("FkUserId")]
    public UserLogin? UserLogin { get; set; }
    public Guid FkUserId { get; set; }

    [ForeignKey("FkRoleId")]
    public AppRole? AppRole { get; set; }
    public Guid FkRoleId { get; set; }
}
