using Domain.Common;
using Domain.Entities.Catalog;
using NibFramework.Model;

namespace Domain.Entities.Sales;

public class OrderItem : BaseEntity<int>
{
    public int OrderId { get; private set; }
    public int ProductId { get; private set; }
    public string ProductName { get; private set; } = null!;
    public string Sku { get; private set; } = null!;
    public int Quantity { get; private set; }
    public decimal PriceAtPurchase { get; private set; }
    public decimal LineTotal { get; private set; }

    #region Relations
    public virtual Order Order { get; private set; } = null!;
    public virtual Product Product { get; private set; } = null!;
    #endregion

    protected OrderItem()
    {
    }

    internal OrderItem(Order order, Product product, int quantity, decimal priceAtPurchase)
    {
        Order = order;
        Product = product;
        if (order.Id != 0)
            OrderId = order.Id;
        if (product.Id != 0)
            ProductId = product.Id;

        ProductName = product.Name;
        Sku = product.Sku;
        Quantity = Guard.Positive(quantity, nameof(quantity));
        PriceAtPurchase = Guard.Money(priceAtPurchase, nameof(priceAtPurchase));
        LineTotal = decimal.Round(PriceAtPurchase * Quantity, 2, MidpointRounding.AwayFromZero);
        CreatedAt = DateTime.UtcNow;
    }
}
