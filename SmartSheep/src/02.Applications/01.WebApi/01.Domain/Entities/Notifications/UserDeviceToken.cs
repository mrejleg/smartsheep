using Api.Domain.Entities.Auths;
using Project.Base.Models.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Domain.Entities.Notifications;

public class UserDeviceToken : BaseEntity<Guid>
{
    [Required]
    public Guid FkUserId { get; set; }

    [ForeignKey(nameof(FkUserId))]
    public UserLogin? UserLogin { get; set; }

    [Required]
    [Column(TypeName = "VARCHAR(512)")]
    [StringLength(512)]
    public string DeviceToken { get; set; } = string.Empty;

    [Column(TypeName = "VARCHAR(20)")]
    [StringLength(20)]
    public string? Platform { get; set; }

    [Column(TypeName = "VARCHAR(100)")]
    [StringLength(100)]
    public string? DeviceName { get; set; }
}
