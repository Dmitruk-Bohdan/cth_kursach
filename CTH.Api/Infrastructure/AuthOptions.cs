using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace CTH.Api.Infrastructure;

public static class AuthOptions
{
    public const string Issuer = "CTH.AuthServer";
    public const string Audience = "CTH.ApiClient";
    private const string Key = "cth-super-secret-signing-key-change-me";
    public const int Lifetime = 2;

    public static SymmetricSecurityKey GetSymmetricSecurityKey()
        => new(Encoding.UTF8.GetBytes(Key));
}
