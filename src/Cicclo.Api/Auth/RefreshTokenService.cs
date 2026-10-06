using System.Security.Cryptography;
using System.Text;
using Cicclo.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cicclo.Api.Auth;

public sealed class RefreshTokenService(AppDbContext db, IOptions<JwtOptions> options)
{
    public async Task<string> Issue(Guid userId, CancellationToken cancellationToken)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        db.RefreshTokens.Add(NewToken(userId, token));
        await db.SaveChangesAsync(cancellationToken);
        return token;
    }

    public async Task<(UserAccount User, string RefreshToken)?> Rotate(string? presented, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(presented))
            return null;

        var hash = Hash(presented);
        var stored = await db.RefreshTokens.FirstOrDefaultAsync(token => token.TokenHash == hash, cancellationToken);
        if (stored is null || stored.RevokedAt is not null || stored.ExpiresAt <= DateTimeOffset.UtcNow)
            return null;

        var user = await db.Users.FirstOrDefaultAsync(account => account.Id == stored.UserId, cancellationToken);
        if (user is null)
            return null;

        stored.RevokedAt = DateTimeOffset.UtcNow;
        var next = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        db.RefreshTokens.Add(NewToken(user.Id, next));
        await db.SaveChangesAsync(cancellationToken);
        return (user, next);
    }

    private RefreshToken NewToken(Guid userId, string token) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        TokenHash = Hash(token),
        ExpiresAt = DateTimeOffset.UtcNow.AddDays(options.Value.RefreshTokenDays),
    };

    private static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
