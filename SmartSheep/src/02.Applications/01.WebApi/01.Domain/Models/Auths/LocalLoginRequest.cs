using System.ComponentModel.DataAnnotations;

namespace Api.Domain.Models.Auths;

public class LocalLoginRequest
{
    [Required] public string Username { get; set; }
    [Required] public string Password { get; set; }
}
