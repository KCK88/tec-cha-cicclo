namespace Cicclo.Api.Data;

public sealed class LaundryService
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public int PriceCents { get; set; }
}
