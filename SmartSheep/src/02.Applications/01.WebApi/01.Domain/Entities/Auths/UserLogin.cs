using Project.Base.Models.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Domain.Entities.Auths;

public class UserLogin : BaseEntity<Guid>
{
    [Required]
    [Column(TypeName = "VARCHAR(100)")]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
    public string Username { get; set; }

    [Required]
    [Column(TypeName = "VARCHAR(512)")]
    [StringLength(512, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
    public string Password { get; set; }

    [Required]
    [Column(TypeName = "VARCHAR(256)")]
    [StringLength(256, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
    public string Email { get; set; }

    [Required]
    [Column(TypeName = "VARCHAR(256)")]
    [StringLength(256, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
    public string FullName { get; set; }

    [Column(TypeName = "VARCHAR(1000)")]
    [StringLength(1000, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
    public string? UserImage { get; set; }

    public DateTime? LatestLogin { get; set; }

    [Column(TypeName = "VARCHAR(50)")]
    [StringLength(50, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
    public string? HP { get; set; }

    public ICollection<UserLoginRole> UserLoginRoles { get; set; } = new List<UserLoginRole>();
}
