namespace Cicclo.Api.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "";
    public string SigningKey { get; set; } = "";
    public int AccessTokenHours { get; set; } = 8;
    public int RefreshTokenDays { get; set; } = 30;
}
