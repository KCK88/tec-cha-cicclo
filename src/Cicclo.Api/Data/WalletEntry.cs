namespace Cicclo.Api.Data;

public sealed class WalletEntry
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Kind { get; set; } = "";
    public int AmountCents { get; set; }
    public Guid? ServiceId { get; set; }
    public string? ServiceName { get; set; }
    public Guid? ReversesEntryId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public bool Cancelled { get; set; }
}
