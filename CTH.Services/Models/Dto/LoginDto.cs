using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CTH.Services.Models.Dto
{
    public class AuthorizeDto
    {
        [Required(AllowEmptyStrings = false)]
        [JsonPropertyName("email")]
        public string Email { get; set; } = default!;

        [Required(AllowEmptyStrings = false)]
        [JsonPropertyName("password")]
        public string Password { get; set; } = default!;
    }
}