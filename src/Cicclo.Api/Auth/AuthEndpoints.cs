using Cicclo.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Cicclo.Api.Auth;

public static class AuthEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/auth");
        group.MapPost("/register", Register);
        group.MapPost("/login", Login);
        group.MapPost("/refresh", Refresh);
    }

    private static async Task<IResult> Register(
        Credentials body,
        AppDbContext db,
        JwtTokenService tokens,
        RefreshTokenService refreshTokens,
        CancellationToken cancellationToken)
    {
        var email = EmailAddress.Normalize(body.Email);
        if (email is null)
            return Results.BadRequest(new { code = "invalid_request", message = "Informe um e-mail válido." });

        if (!IsPassword(body.Password))
            return Results.BadRequest(new { code = "invalid_request", message = "A senha precisa ter de 8 a 128 caracteres." });

        if (await db.Users.AnyAsync(user => user.Email == email, cancellationToken))
            return Results.Conflict(new { code = "email_taken", message = "Esse e-mail já está cadastrado." });

        var user = new UserAccount
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = PasswordHasher.Hash(body.Password!),
            BalanceCents = WalletRules.InitialBalanceCents,
        };
        db.Users.Add(user);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Results.Conflict(new { code = "email_taken", message = "Esse e-mail já está cadastrado." });
        }

        var refreshToken = await refreshTokens.Issue(user.Id, cancellationToken);
        return Results.Created("/wallet", TokenBody(tokens.Create(user), refreshToken));
    }

    private static async Task<IResult> Login(
        Credentials body,
        AppDbContext db,
        JwtTokenService tokens,
        RefreshTokenService refreshTokens,
        CancellationToken cancellationToken)
    {
        var email = EmailAddress.Normalize(body.Email);
        var user = email is null
            ? null
            : await db.Users.FirstOrDefaultAsync(account => account.Email == email, cancellationToken);

        if (user is null || body.Password is null || !PasswordHasher.Verify(body.Password, user.PasswordHash))
            return Results.Json(new { code = "invalid_credentials" }, statusCode: StatusCodes.Status401Unauthorized);

        var refreshToken = await refreshTokens.Issue(user.Id, cancellationToken);
        return Results.Ok(TokenBody(tokens.Create(user), refreshToken));
    }

    private static async Task<IResult> Refresh(
        RefreshRequest body,
        JwtTokenService tokens,
        RefreshTokenService refreshTokens,
        CancellationToken cancellationToken)
    {
        var rotated = await refreshTokens.Rotate(body.RefreshToken, cancellationToken);
        if (rotated is null)
            return Results.Json(new { code = "invalid_refresh_token" }, statusCode: StatusCodes.Status401Unauthorized);

        return Results.Ok(TokenBody(tokens.Create(rotated.Value.User), rotated.Value.RefreshToken));
    }

    internal static object TokenBody(AccessToken access, string refreshToken) => new
    {
        accessToken = access.Token,
        refreshToken,
        expiresIn = access.ExpiresIn,
    };

    private static bool IsPassword(string? password) =>
        password is { Length: >= 8 and <= 128 };

    public sealed record Credentials(string? Email, string? Password);
    public sealed record RefreshRequest(string? RefreshToken);
}
