using Domain.Common;
using Domain.Entities.Customers;
using NibFramework.Model;

namespace Domain.Entities.Catalog;

public class Review : BaseEntity<int>
{
    public int ProductId { get; private set; }
    public int CustomerId { get; private set; }
    public byte Rating { get; private set; }
    public string? Title { get; private set; }
    public string Comment { get; private set; } = null!;
    public bool IsApproved { get; private set; }
    public bool IsDeleted { get; private set; }

    #region Relations
    public virtual Product Product { get; private set; } = null!;
    public virtual Customer Customer { get; private set; } = null!;
    #endregion

    protected Review()
    {
    }

    internal Review(Product product, Customer customer, byte rating, string comment, string? title)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(customer);

        Product = product;
        Customer = customer;
        if (product.Id != 0)
            ProductId = product.Id;
        if (customer.Id != 0)
            CustomerId = customer.Id;

        SetContent(rating, comment, title);
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(byte rating, string comment, string? title = null)
    {
        if (IsDeleted)
            throw new DomainException("A deleted review cannot be edited.");

        SetContent(rating, comment, title);
        IsApproved = false;
        this.Touch();
    }

    public void Approve()
    {
        if (IsDeleted)
            throw new DomainException("A deleted review cannot be approved.");

        IsApproved = true;
        this.Touch();
    }

    public void Reject()
    {
        IsApproved = false;
        this.Touch();
    }

    public void Delete()
    {
        if (IsDeleted)
            return;

        IsDeleted = true;
        IsApproved = false;
        this.Touch();
    }

    private void SetContent(byte rating, string comment, string? title)
    {
        Rating = Guard.Rating(rating);
        Comment = Guard.Required(comment, nameof(comment), FieldLengths.Comment);
        Title = Guard.Optional(title, nameof(title), FieldLengths.Title);
    }
}
