using Domain.Common;
using Domain.Entities.Catalog;
using Domain.Entities.Customers;
using NibFramework.Model;

namespace Domain.Entities.Sales;

public class Cart : BaseEntity<int>
{
    public int CustomerId { get; private set; }

    #region Relations
    public virtual Customer Customer { get; private set; } = null!;
    public virtual ICollection<CartItem> Items { get; private set; } = new List<CartItem>();
    #endregion

    protected Cart()
    {
    }

    internal Cart(Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);
        Customer = customer;
        if (customer.Id != 0)
            CustomerId = customer.Id;
        CreatedAt = DateTime.UtcNow;
    }

    public CartItem AddItem(Product product, int quantity)
    {
        ArgumentNullException.ThrowIfNull(product);
        var amount = Guard.Positive(quantity, nameof(quantity));
        EnsureSellable(product, amount);

        var price = product.CalculateDiscountPrice();
        var existing = FindItem(product);
        if (existing is null)
        {
            var item = new CartItem(this, product, amount, price);
            Items.Add(item);
            this.Touch();
            return item;
        }

        var nextQuantity = existing.Quantity + amount;
        if (nextQuantity > product.StockQuantity)
            throw new DomainException($"Not enough stock for '{product.Name}'.");

        existing.Increase(amount, price);
        this.Touch();
        return existing;
    }

    public void ChangeItemQuantity(Product product, int quantity)
    {
        ArgumentNullException.ThrowIfNull(product);
        var item = FindItem(product) ?? throw new DomainException("Item is not in the cart.");
        var amount = Guard.Positive(quantity, nameof(quantity));
        if (amount > product.StockQuantity)
            throw new DomainException($"Not enough stock for '{product.Name}'.");

        item.ChangeQuantity(amount, product.CalculateDiscountPrice());
        this.Touch();
    }

    public void RemoveItem(Product product)
    {
        var item = FindItem(product) ?? throw new DomainException("Item is not in the cart.");
        Items.Remove(item);
        this.Touch();
    }

    public void Clear()
    {
        Items.Clear();
        this.Touch();
    }

    /// <summary>
    /// Rewrites line prices from the current product price. Products must be loaded.
    /// </summary>
    public void RefreshPrices()
    {
        foreach (var item in Items)
        {
            if (item.Product is null)
                throw new DomainException("Load cart items with their products before refreshing prices.");

            item.SetPrice(item.Product.CalculateDiscountPrice());
        }

        this.Touch();
    }

    public decimal GetTotal() => Items.Sum(i => i.LineTotal);

    private CartItem? FindItem(Product product)
        => Items.FirstOrDefault(i =>
            ReferenceEquals(i.Product, product) ||
            (product.Id != 0 && i.ProductId == product.Id));

    private static void EnsureSellable(Product product, int quantity)
    {
        if (!product.IsActive)
            throw new DomainException($"'{product.Name}' is not available.");

        if (quantity > product.StockQuantity)
            throw new DomainException($"Not enough stock for '{product.Name}'.");
    }
}
