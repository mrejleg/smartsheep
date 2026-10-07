using Microsoft.AspNetCore.Http;

namespace Api.Domain.Models.Auths
{
    public class AppUserProfileDto
    {
        public string Username { get; set; }
        public IFormFile? FilePhoto { get; set; }
        public string? PhotoBase64 { get; set; }
        public string? MimeType { get; set; }
        public string? FileExtention { get; set; }
    }
}
