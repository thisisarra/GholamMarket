using Domain.Common;
using Domain.Entities.Catalog;
using Domain.Entities.Customers;
using NibFramework.Model;

namespace Domain.Entities.Sales;

public class CartItem : BaseEntity<int>
{
    public int CartId { get; private set; }
    public int ProductId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal LineTotal { get; private set; }

    #region Relations
    public virtual Cart Cart { get; private set; } = null!;
    public virtual Product Product { get; private set; } = null!;
    #endregion

    protected CartItem()
    {
    }

    internal CartItem(Cart cart, Product product, int quantity, decimal unitPrice)
    {
        Cart = cart;
        Product = product;
        if (cart.Id != 0)
            CartId = cart.Id;
        if (product.Id != 0)
            ProductId = product.Id;

        Quantity = Guard.Positive(quantity, nameof(quantity));
        UnitPrice = Guard.Money(unitPrice, nameof(unitPrice));
        LineTotal = decimal.Round(UnitPrice * Quantity, 2, MidpointRounding.AwayFromZero);
        CreatedAt = DateTime.UtcNow;
    }

    internal void Increase(int quantity, decimal unitPrice)
    {
        Quantity += Guard.Positive(quantity, nameof(quantity));
        SetPrice(unitPrice);
    }

    internal void ChangeQuantity(int quantity, decimal unitPrice)
    {
        Quantity = Guard.Positive(quantity, nameof(quantity));
        SetPrice(unitPrice);
    }

    internal void SetPrice(decimal unitPrice)
    {
        UnitPrice = Guard.Money(unitPrice, nameof(unitPrice));
        LineTotal = decimal.Round(UnitPrice * Quantity, 2, MidpointRounding.AwayFromZero);
        this.Touch();
    }
}
