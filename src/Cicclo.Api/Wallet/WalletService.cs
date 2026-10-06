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

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
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
            await transaction.RollbackAsync(cancellationToken);
            return balance is null
                ? PurchaseResult.MissingUser()
                : PurchaseResult.Insufficient(balance.Value, service.PriceCents);
        }

        db.Entries.Add(new WalletEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Kind = EntryKind.Purchase,
            AmountCents = service.PriceCents,
            ServiceId = service.Id,
            ServiceName = service.Name,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(cancellationToken);

        var balanceAfter = await db.Users.AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => user.BalanceCents)
            .SingleAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return PurchaseResult.Ok(service, balanceAfter);
    }

    public async Task<CreditResult> TopUp(Guid userId, int amountCents, CancellationToken cancellationToken)
    {
        if (amountCents <= 0 || amountCents > 1_000_000)
            return CreditResult.Invalid();

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var affected = await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE users
            SET balance_cents = balance_cents + {amountCents}
            WHERE id = {userId} AND balance_cents <= {int.MaxValue - amountCents}
            """, cancellationToken);

        if (affected == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            var exists = await db.Users.AsNoTracking().AnyAsync(user => user.Id == userId, cancellationToken);
            return exists ? CreditResult.Invalid() : CreditResult.MissingUser();
        }

        db.Entries.Add(new WalletEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Kind = EntryKind.TopUp,
            AmountCents = amountCents,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(cancellationToken);

        var balance = await db.Users.AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => user.BalanceCents)
            .SingleAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return CreditResult.Ok(balance);
    }

    public async Task<CreditResult> Cancel(Guid userId, Guid entryId, CancellationToken cancellationToken)
    {
        var entry = await db.Entries.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == entryId && item.UserId == userId, cancellationToken);
        if (entry is null)
            return CreditResult.MissingEntry();
        if (entry.Kind != EntryKind.Purchase)
            return CreditResult.NotAPurchase();

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var marked = await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE wallet_entries
            SET cancelled = true
            WHERE id = {entryId} AND user_id = {userId} AND kind = {EntryKind.Purchase} AND cancelled = false
            """, cancellationToken);
        if (marked == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return CreditResult.AlreadyCancelled();
        }

        var credited = await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE users
            SET balance_cents = balance_cents + {entry.AmountCents}
            WHERE id = {userId} AND balance_cents <= {int.MaxValue - entry.AmountCents}
            """, cancellationToken);
        if (credited == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return CreditResult.Invalid();
        }

        db.Entries.Add(new WalletEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Kind = EntryKind.Cancellation,
            AmountCents = entry.AmountCents,
            ServiceId = entry.ServiceId,
            ServiceName = entry.ServiceName,
            ReversesEntryId = entry.Id,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(cancellationToken);

        var balance = await db.Users.AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => user.BalanceCents)
            .SingleAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return CreditResult.Ok(balance);
    }

    public Task<List<WalletEntry>> Entries(Guid userId, CancellationToken cancellationToken) =>
        db.Entries.AsNoTracking()
            .Where(entry => entry.UserId == userId)
            .OrderByDescending(entry => entry.CreatedAt)
            .ToListAsync(cancellationToken);
}

public enum CreditStatus
{
    Ok,
    Invalid,
    MissingUser,
    MissingEntry,
    NotAPurchase,
    AlreadyCancelled,
}

public sealed record CreditResult(CreditStatus Status, int BalanceCents)
{
    public static CreditResult Ok(int balanceCents) => new(CreditStatus.Ok, balanceCents);
    public static CreditResult Invalid() => new(CreditStatus.Invalid, 0);
    public static CreditResult MissingUser() => new(CreditStatus.MissingUser, 0);
    public static CreditResult MissingEntry() => new(CreditStatus.MissingEntry, 0);
    public static CreditResult NotAPurchase() => new(CreditStatus.NotAPurchase, 0);
    public static CreditResult AlreadyCancelled() => new(CreditStatus.AlreadyCancelled, 0);
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
