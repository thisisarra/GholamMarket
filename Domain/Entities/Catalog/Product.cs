using Domain.Common;
using Domain.Entities.Customers;
using NibFramework.Model;

namespace Domain.Entities.Catalog;

public class Product : BaseEntity<int>
{
    public int CategoryId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public string? Description { get; private set; }
    public decimal Price { get; private set; }
    public decimal? DiscountPercent { get; private set; }
    public int StockQuantity { get; private set; }
    public string Sku { get; private set; } = null!;
    public string? ImageUrl { get; private set; }
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    #region Relations
    public virtual Category Category { get; private set; } = null!;
    public virtual ICollection<ProductAttribute> Attributes { get; private set; } = new List<ProductAttribute>();
    public virtual ICollection<Review> Reviews { get; private set; } = new List<Review>();
    #endregion

    protected Product()
    {
    }

    public Product(
        Category category,
        string name,
        string slug,
        decimal price,
        int stockQuantity,
        string sku,
        string? description = null,
        string? imageUrl = null)
    {
        ArgumentNullException.ThrowIfNull(category);
        Category = category;
        if (category.Id != 0)
            CategoryId = category.Id;

        SetDetails(name, slug, price, sku, description, imageUrl, isActive: true);
        if (stockQuantity < 0)
            throw new ArgumentOutOfRangeException(nameof(stockQuantity), "Stock cannot be negative.");

        StockQuantity = stockQuantity;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(
        string name,
        string slug,
        decimal price,
        string sku,
        string? description,
        string? imageUrl,
        bool isActive)
    {
        SetDetails(name, slug, price, sku, description, imageUrl, isActive);
        this.Touch();
    }

    public void MoveTo(Category category)
    {
        ArgumentNullException.ThrowIfNull(category);
        Category = category;
        if (category.Id != 0)
            CategoryId = category.Id;
        this.Touch();
    }

    public void UpdateStock(int quantity)
    {
        if (quantity < 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Stock cannot be negative.");

        StockQuantity = quantity;
        this.Touch();
    }

    public void IncreaseStock(int quantity)
    {
        StockQuantity += Guard.Positive(quantity, nameof(quantity));
        this.Touch();
    }

    public void DecreaseStock(int quantity)
    {
        var amount = Guard.Positive(quantity, nameof(quantity));
        if (StockQuantity < amount)
            throw new DomainException($"Not enough stock for '{Name}'. Available: {StockQuantity}, requested: {amount}.");

        StockQuantity -= amount;
        this.Touch();
    }

    public void SetDiscount(decimal? percent)
    {
        if (percent is null or 0)
        {
            DiscountPercent = null;
            this.Touch();
            return;
        }

        if (percent is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(percent), "Discount percent must be between 0 and 100.");

        DiscountPercent = decimal.Round(percent.Value, 2, MidpointRounding.AwayFromZero);
        this.Touch();
    }

    public decimal CalculateDiscountPrice()
    {
        if (DiscountPercent is null or 0)
            return Price;

        return decimal.Round(Price * (1 - DiscountPercent.Value / 100m), 2, MidpointRounding.AwayFromZero);
    }

    public ProductAttribute AddAttribute(string name, string value)
    {
        var normalized = name.Trim();
        if (Attributes.Any(a => a.Name.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
            throw new DomainException($"Attribute '{normalized}' already exists on this product.");

        var attribute = new ProductAttribute(this, normalized, value, Attributes.Count);
        Attributes.Add(attribute);
        return attribute;
    }

    public Review AddReview(Customer customer, byte rating, string comment, string? title = null)
    {
        ArgumentNullException.ThrowIfNull(customer);
        if (Reviews.Any(r => !r.IsDeleted && customer.Id != 0 && r.CustomerId == customer.Id))
            throw new DomainException("This customer has already reviewed the product.");

        var review = new Review(this, customer, rating, comment, title);
        Reviews.Add(review);
        customer.Reviews.Add(review);
        return review;
    }

    private void SetDetails(
        string name,
        string slug,
        decimal price,
        string sku,
        string? description,
        string? imageUrl,
        bool isActive)
    {
        Name = Guard.Required(name, nameof(name), FieldLengths.Name);
        Slug = Guard.Slug(slug, nameof(slug));
        Price = Guard.Money(price, nameof(price));
        Sku = Guard.Required(sku, nameof(sku), FieldLengths.Sku);
        Description = Guard.Optional(description, nameof(description), FieldLengths.Description);
        ImageUrl = Guard.Optional(imageUrl, nameof(imageUrl), FieldLengths.Url);
        IsActive = isActive;
    }
}
