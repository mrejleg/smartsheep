using System.ComponentModel.DataAnnotations;

namespace Project.Base.Models.Jwts
{
    public class TokenRequest
    {
        [Required]
        public string ClientId { get; set; }
        [Required]
        [DataType(DataType.Password)]
        public string ClientSecret { get; set; }
    }
}
