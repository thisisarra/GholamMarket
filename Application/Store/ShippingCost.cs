namespace Application.Store;

public static class ShippingCost
{
    public const decimal FreeThreshold = 2_000_000m;
    public const decimal FlatRate = 85_000m;

    public static decimal For(decimal subtotal)
        => subtotal >= FreeThreshold ? 0 : FlatRate;
}
