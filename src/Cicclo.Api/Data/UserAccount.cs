namespace Cicclo.Api.Data;

public sealed class UserAccount
{
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public int BalanceCents { get; set; }
}
