using Cicclo.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Cicclo.Api.Wallet;

public sealed class WalletService(AppDbContext db)
{
    public async Task<PurchaseResult> Purchase(Guid userId, Guid serviceId, CancellationToken cancellationToken)
    {
        var service = await db.Services.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == serviceId, cancellationToken);
        if (service is null)
            return PurchaseResult.MissingService();

        var affected = await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE users
            SET balance_cents = balance_cents - {service.PriceCents}
            WHERE id = {userId} AND balance_cents >= {service.PriceCents}
            """, cancellationToken);

        if (affected == 0)
        {
            var balance = await db.Users.AsNoTracking()
                .Where(user => user.Id == userId)
                .Select(user => (int?)user.BalanceCents)
                .SingleOrDefaultAsync(cancellationToken);
            return balance is null
                ? PurchaseResult.MissingUser()
                : PurchaseResult.Insufficient(balance.Value, service.PriceCents);
        }

        var balanceAfter = await db.Users.AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => user.BalanceCents)
            .SingleAsync(cancellationToken);

        return PurchaseResult.Ok(service, balanceAfter);
    }
}

public sealed record PurchaseResult(
    PurchaseStatus Status,
    LaundryService? Service,
    int BalanceCents,
    int PriceCents)
{
    public static PurchaseResult Ok(LaundryService service, int balanceCents) =>
        new(PurchaseStatus.Ok, service, balanceCents, service.PriceCents);

    public static PurchaseResult Insufficient(int balanceCents, int priceCents) =>
        new(PurchaseStatus.Insufficient, null, balanceCents, priceCents);

    public static PurchaseResult MissingService() =>
        new(PurchaseStatus.MissingService, null, 0, 0);

    public static PurchaseResult MissingUser() =>
        new(PurchaseStatus.MissingUser, null, 0, 0);
}

public enum PurchaseStatus
{
    Ok,
    Insufficient,
    MissingService,
    MissingUser,
}
