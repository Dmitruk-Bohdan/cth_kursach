using System.Text.Json.Serialization;

namespace CTH.Services.Models.ResponseModels;

public class LoginResponseModel
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = default!;

    [JsonPropertyName("userName")]
    public string UserName { get; set; } = default!;
}
