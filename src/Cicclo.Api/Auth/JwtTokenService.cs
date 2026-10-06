using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Cicclo.Api.Data;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Cicclo.Api.Auth;

public sealed class JwtTokenService(IOptions<JwtOptions> options)
{
    public AccessToken Create(UserAccount user)
    {
        var jwt = options.Value;
        var expiresIn = (int)TimeSpan.FromHours(jwt.AccessTokenHours).TotalSeconds;
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
            ]),
            Expires = DateTime.UtcNow.AddSeconds(expiresIn),
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                SecurityAlgorithms.HmacSha256),
        };

        var handler = new JwtSecurityTokenHandler();
        return new AccessToken(handler.WriteToken(handler.CreateToken(descriptor)), expiresIn);
    }
}

public sealed record AccessToken(string Token, int ExpiresIn);
