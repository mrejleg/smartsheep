using System.ComponentModel.DataAnnotations;

namespace Api.Domain.Models.Auths
{
    public class MobileLoginRequest
    {
        [Required]
        public string ClientId { get; set; }

        [Required]
        public string ClientSecret { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }

        public string? DeviceToken { get; set; }

        public string? Platform { get; set; }

        public string? DeviceName { get; set; }
    }
}
