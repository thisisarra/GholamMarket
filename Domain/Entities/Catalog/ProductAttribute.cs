using Domain.Common;
using NibFramework.Model;

namespace Domain.Entities.Catalog;

public class ProductAttribute : BaseEntity<int>
{
    public int ProductId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Value { get; private set; } = null!;
    public int DisplayOrder { get; private set; }

    public virtual Product Product { get; private set; } = null!;

    protected ProductAttribute()
    {
    }

    internal ProductAttribute(Product product, string name, string value, int displayOrder)
    {
        ArgumentNullException.ThrowIfNull(product);
        Product = product;
        if (product.Id != 0)
            ProductId = product.Id;

        Name = Guard.Required(name, nameof(name), FieldLengths.Name);
        Value = Guard.Required(value, nameof(value), FieldLengths.Value);
        DisplayOrder = displayOrder;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string name, string value, int displayOrder = 0)
    {
        if (displayOrder < 0)
            throw new ArgumentOutOfRangeException(nameof(displayOrder));

        Name = Guard.Required(name, nameof(name), FieldLengths.Name);
        Value = Guard.Required(value, nameof(value), FieldLengths.Value);
        DisplayOrder = displayOrder;
        this.Touch();
    }
}
