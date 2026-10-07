using System.ComponentModel.DataAnnotations;

namespace Web.Domain.Models.Configs;

public class UserFarm
{
    public Guid Id { get; set; }
    [Required] public Guid? FkUserId { get; set; }
    [Required] public Guid? FkFarmId { get; set; }
    public string? Username { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? FarmCode { get; set; }
    public string? FarmName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? DateCreated { get; set; }
    public DateTime? DateModified { get; set; }
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
}
