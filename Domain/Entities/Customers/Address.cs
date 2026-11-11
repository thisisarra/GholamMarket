using Domain.Common;
using NibFramework.Model;

namespace Domain.Entities.Customers;

/// <summary>
/// A saved delivery address. Orders copy these fields at checkout so later edits do not rewrite history.
/// </summary>
public class Address : BaseEntity<int>
{
    public int CustomerId { get; private set; }
    public string Title { get; private set; } = null!;
    public string ReceiverName { get; private set; } = null!;
    public string ReceiverPhone { get; private set; } = null!;
    public string Province { get; private set; } = null!;
    public string City { get; private set; } = null!;
    public string Street { get; private set; } = null!;
    public string? Plaque { get; private set; }
    public string? Unit { get; private set; }
    public string PostalCode { get; private set; } = null!;
    public bool IsDefault { get; private set; }
    public bool IsDeleted { get; private set; }

    public virtual Customer Customer { get; private set; } = null!;

    protected Address()
    {
    }

    internal Address(
        Customer customer,
        string title,
        string receiverName,
        string receiverPhone,
        string province,
        string city,
        string street,
        string postalCode,
        string? plaque,
        string? unit)
    {
        ArgumentNullException.ThrowIfNull(customer);
        Customer = customer;
        if (customer.Id != 0)
            CustomerId = customer.Id;

        SetDetails(title, receiverName, receiverPhone, province, city, street, postalCode, plaque, unit);
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(
        string title,
        string receiverName,
        string receiverPhone,
        string province,
        string city,
        string street,
        string postalCode,
        string? plaque = null,
        string? unit = null)
    {
        if (IsDeleted)
            throw new DomainException("A deleted address cannot be edited.");

        SetDetails(title, receiverName, receiverPhone, province, city, street, postalCode, plaque, unit);
        this.Touch();
    }

    public void MarkAsDefault()
    {
        if (IsDeleted)
            throw new DomainException("A deleted address cannot be the default.");

        IsDefault = true;
        this.Touch();

        if (Customer is null)
            return;

        foreach (var address in Customer.Addresses)
        {
            if (!ReferenceEquals(address, this))
                address.UnsetDefault();
        }
    }

    public void Delete()
    {
        if (IsDeleted)
            return;

        var wasDefault = IsDefault;
        IsDeleted = true;
        IsDefault = false;
        this.Touch();

        if (!wasDefault || Customer is null)
            return;

        Customer.Addresses.FirstOrDefault(a => !a.IsDeleted)?.MarkAsDefault();
    }

    internal void UnsetDefault()
    {
        if (!IsDefault)
            return;

        IsDefault = false;
        this.Touch();
    }

    private void SetDetails(
        string title,
        string receiverName,
        string receiverPhone,
        string province,
        string city,
        string street,
        string postalCode,
        string? plaque,
        string? unit)
    {
        Title = Guard.Required(title, nameof(title), FieldLengths.Title);
        ReceiverName = Guard.Required(receiverName, nameof(receiverName), FieldLengths.Name);
        ReceiverPhone = Guard.Required(receiverPhone, nameof(receiverPhone), FieldLengths.Phone);
        Province = Guard.Required(province, nameof(province), FieldLengths.Province);
        City = Guard.Required(city, nameof(city), FieldLengths.City);
        Street = Guard.Required(street, nameof(street), FieldLengths.Street);
        PostalCode = Guard.Required(postalCode, nameof(postalCode), FieldLengths.PostalCode);
        Plaque = Guard.Optional(plaque, nameof(plaque), FieldLengths.Plaque);
        Unit = Guard.Optional(unit, nameof(unit), FieldLengths.Plaque);
    }
}
