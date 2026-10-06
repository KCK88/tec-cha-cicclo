using Cicclo.Api.Auth;
using Cicclo.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Cicclo.Api.Wallet;

public static class WalletEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/services", ListServices).RequireAuthorization();
        app.MapGet("/wallet", GetWallet).RequireAuthorization();
        app.MapPost("/services/{id:guid}/purchases", Purchase).RequireAuthorization();
    }

    private static async Task<IResult> ListServices(AppDbContext db, CancellationToken cancellationToken)
    {
        var services = await db.Services.AsNoTracking()
            .OrderBy(service => service.Name)
            .Select(service => new
            {
                id = service.Id,
                name = service.Name,
                priceCents = service.PriceCents,
            })
            .ToListAsync(cancellationToken);

        return Results.Ok(services);
    }

    private static async Task<IResult> GetWallet(HttpContext http, AppDbContext db, CancellationToken cancellationToken)
    {
        var userId = CurrentUser.Id(http);
        if (userId is null)
            return Results.Unauthorized();

        var balance = await db.Users.AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => (int?)user.BalanceCents)
            .SingleOrDefaultAsync(cancellationToken);

        return balance is null
            ? Results.Unauthorized()
            : Results.Ok(new { balanceCents = balance.Value });
    }

    private static async Task<IResult> Purchase(
        Guid id,
        HttpContext http,
        WalletService wallet,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUser.Id(http);
        if (userId is null)
            return Results.Unauthorized();

        var result = await wallet.Purchase(userId.Value, id, cancellationToken);
        return result.Status switch
        {
            PurchaseStatus.MissingService => Results.NotFound(new { code = "service_not_found" }),
            PurchaseStatus.MissingUser => Results.Unauthorized(),
            PurchaseStatus.Insufficient => Results.Conflict(new
            {
                code = "insufficient_balance",
                balanceCents = result.BalanceCents,
                priceCents = result.PriceCents,
            }),
            _ => Results.Ok(new
            {
                serviceId = result.Service!.Id,
                serviceName = result.Service.Name,
                priceCents = result.PriceCents,
                balanceCents = result.BalanceCents,
            }),
        };
    }
}
