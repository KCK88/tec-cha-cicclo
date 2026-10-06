using Cicclo.Api.Auth;
using Microsoft.EntityFrameworkCore;

namespace Cicclo.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, IConfiguration configuration, CancellationToken cancellationToken = default)
    {
        if (!await db.Services.AnyAsync(cancellationToken))
        {
            db.Services.AddRange(
                new LaundryService { Id = Catalog.WashId, Name = "Lavagem", PriceCents = 1_890 },
                new LaundryService { Id = Catalog.DryId, Name = "Secagem", PriceCents = 2_090 });
        }

        var email = (configuration["Seed:Email"] ?? "usuario@cicclo.dev").Trim().ToLowerInvariant();
        var password = configuration["Seed:Password"]
            ?? throw new InvalidOperationException("Seed:Password is required.");

        if (!await db.Users.AnyAsync(user => user.Email == email, cancellationToken))
        {
            db.Users.Add(new UserAccount
            {
                Id = Guid.NewGuid(),
                Email = email,
                PasswordHash = PasswordHasher.Hash(password),
                BalanceCents = WalletRules.InitialBalanceCents,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
