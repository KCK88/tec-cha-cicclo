using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Cicclo.Api.Data;
using Npgsql;

namespace Cicclo.Api.Tests;

[Collection(ApiCollection.Name)]
public sealed class LaundryApiTests(ApiFactory factory)
{
    [Fact]
    public async Task Register_starts_with_fifty_reais()
    {
        var client = await RegisterAsync();

        var wallet = await client.GetFromJsonAsync<WalletBody>("/wallet");

        Assert.Equal(5_000, wallet!.BalanceCents);
    }

    [Fact]
    public async Task Register_rejects_the_same_email_ignoring_case()
    {
        var email = $"pessoa-{Guid.NewGuid():N}@cicclo.dev";
        var client = factory.CreateClient();

        var first = await client.PostAsJsonAsync("/auth/register", new { email, password = "senha-segura" });
        var second = await client.PostAsJsonAsync("/auth/register", new { email = email.ToUpperInvariant(), password = "senha-segura" });

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Login_rejects_a_wrong_password()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/auth/login", new
        {
            email = "usuario@cicclo.dev",
            password = "senha-errada",
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Wash_then_dry_leaves_ten_reais_and_twenty_cents()
    {
        var client = await RegisterAsync();

        var wash = await client.PostAsync($"/services/{Catalog.WashId}/purchases", null);
        var dry = await client.PostAsync($"/services/{Catalog.DryId}/purchases", null);
        var wallet = await client.GetFromJsonAsync<WalletBody>("/wallet");

        Assert.Equal(HttpStatusCode.OK, wash.StatusCode);
        Assert.Equal(HttpStatusCode.OK, dry.StatusCode);
        Assert.Equal(1_020, wallet!.BalanceCents);
    }

    [Fact]
    public async Task Third_purchase_is_refused_and_the_balance_stays()
    {
        var client = await RegisterAsync();
        await client.PostAsync($"/services/{Catalog.WashId}/purchases", null);
        await client.PostAsync($"/services/{Catalog.DryId}/purchases", null);

        var refused = await client.PostAsync($"/services/{Catalog.WashId}/purchases", null);
        var body = await refused.Content.ReadFromJsonAsync<BalanceError>();
        var wallet = await client.GetFromJsonAsync<WalletBody>("/wallet");

        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
        Assert.Equal("insufficient_balance", body!.Code);
        Assert.Equal(1_020, body.BalanceCents);
        Assert.Equal(1_890, body.PriceCents);
        Assert.Equal(1_020, wallet!.BalanceCents);
    }

    [Fact]
    public async Task Unknown_service_is_not_found()
    {
        var client = await RegisterAsync();

        var response = await client.PostAsync($"/services/{Guid.NewGuid()}/purchases", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Two_purchases_at_once_cannot_spend_the_same_balance()
    {
        var (client, email) = await RegisterWithEmailAsync();
        await SetBalanceAsync(email, 1_890);

        var first = client.PostAsync($"/services/{Catalog.WashId}/purchases", null);
        var second = client.PostAsync($"/services/{Catalog.WashId}/purchases", null);
        var responses = await Task.WhenAll(first, second);
        var codes = responses.Select(response => response.StatusCode).OrderBy(code => code).ToArray();
        var wallet = await client.GetFromJsonAsync<WalletBody>("/wallet");

        Assert.Equal([HttpStatusCode.OK, HttpStatusCode.Conflict], codes);
        Assert.Equal(0, wallet!.BalanceCents);
    }

    [Fact]
    public async Task Top_up_and_purchase_show_up_in_the_statement()
    {
        var client = await RegisterAsync();

        var topped = await client.PostAsJsonAsync("/wallet/top-ups", new { amountCents = 1_000 });
        var purchase = await client.PostAsync($"/services/{Catalog.WashId}/purchases", null);
        var entries = await client.GetFromJsonAsync<EntryBody[]>("/wallet/entries");
        var wallet = await client.GetFromJsonAsync<WalletBody>("/wallet");

        Assert.Equal(HttpStatusCode.OK, topped.StatusCode);
        Assert.Equal(HttpStatusCode.OK, purchase.StatusCode);
        Assert.Equal(5_000 + 1_000 - 1_890, wallet!.BalanceCents);
        Assert.Contains(entries!, entry => entry.Kind == "top_up" && entry.AmountCents == 1_000);
        Assert.Contains(entries!, entry => entry.Kind == "purchase" && entry.ServiceName == "Lavagem" && !entry.Cancelled);
    }

    [Fact]
    public async Task Cancelling_a_purchase_returns_the_cents_once()
    {
        var client = await RegisterAsync();
        await client.PostAsync($"/services/{Catalog.WashId}/purchases", null);
        var entries = await client.GetFromJsonAsync<EntryBody[]>("/wallet/entries");
        var purchase = entries!.Single(entry => entry.Kind == "purchase");

        var first = await client.PostAsync($"/wallet/entries/{purchase.Id}/cancellations", null);
        var second = await client.PostAsync($"/wallet/entries/{purchase.Id}/cancellations", null);
        var wallet = await client.GetFromJsonAsync<WalletBody>("/wallet");

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal(5_000, wallet!.BalanceCents);
    }

    [Fact]
    public async Task Refresh_rotates_the_token_and_rejects_the_old_one()
    {
        var email = $"refresh-{Guid.NewGuid():N}@cicclo.dev";
        var client = factory.CreateClient();
        var registered = await client.PostAsJsonAsync("/auth/register", new { email, password = "senha-segura" });
        var auth = await registered.Content.ReadFromJsonAsync<AuthBody>();

        var refreshed = await client.PostAsJsonAsync("/auth/refresh", new { refreshToken = auth!.RefreshToken });
        var next = await refreshed.Content.ReadFromJsonAsync<AuthBody>();
        var reused = await client.PostAsJsonAsync("/auth/refresh", new { refreshToken = auth.RefreshToken });

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", next!.AccessToken);
        var wallet = await client.GetAsync("/wallet");

        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, reused.StatusCode);
        Assert.Equal(HttpStatusCode.OK, wallet.StatusCode);
    }

    private async Task<HttpClient> RegisterAsync()
    {
        var (client, _) = await RegisterWithEmailAsync();
        return client;
    }

    private async Task<(HttpClient Client, string Email)> RegisterWithEmailAsync()
    {
        var email = $"user-{Guid.NewGuid():N}@cicclo.dev";
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/auth/register", new { email, password = "senha-segura" });
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthBody>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return (client, email);
    }

    private async Task SetBalanceAsync(string email, int balanceCents)
    {
        await using var connection = new NpgsqlConnection(factory.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "UPDATE users SET balance_cents = @balance WHERE email = @email",
            connection);
        command.Parameters.AddWithValue("balance", balanceCents);
        command.Parameters.AddWithValue("email", email);
        var updated = await command.ExecuteNonQueryAsync();
        Assert.Equal(1, updated);
    }

    private sealed record AuthBody(string AccessToken, string RefreshToken, int ExpiresIn);
    private sealed record EntryBody(Guid Id, string Kind, int AmountCents, string? ServiceName, bool Cancelled);
    private sealed record WalletBody(int BalanceCents);
    private sealed record BalanceError(string Code, int BalanceCents, int PriceCents);
}
