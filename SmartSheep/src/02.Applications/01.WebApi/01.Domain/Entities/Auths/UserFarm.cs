using Api.Domain.Entities.MasterDatas;
using Project.Base.Models.Entities;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Api.Domain.Entities.Auths;

public class UserFarm : BaseEntity<Guid>
{
    public Guid FkUserId { get; set; }
    public Guid FkFarmId { get; set; }

    [ForeignKey(nameof(FkUserId)), JsonIgnore]
    public UserLogin? UserLogin { get; set; }

    [ForeignKey(nameof(FkFarmId)), JsonIgnore]
    public Farm? Farm { get; set; }
}
