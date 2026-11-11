using Domain.Common;
using Domain.Entities.Catalog;
using Domain.Entities.Sales;
using Domain.Entities.Users;
using NibFramework.Model;

namespace Domain.Entities.Customers;

/// <summary>
/// Shopping profile for an authenticated <see cref="User"/>.
/// Login, password, email, and phone stay on the identity user.
/// </summary>
public class Customer : BaseEntity<int>
{
    public string UserId { get; private set; } = null!;
    public string FullName { get; private set; } = null!;
    public int LoyaltyPoints { get; private set; }
    public bool IsActive { get; private set; }

    #region Relations
    public virtual User User { get; private set; } = null!;
    public virtual ICollection<Address> Addresses { get; private set; } = new List<Address>();
    public virtual Cart? Cart { get; private set; }
    public virtual ICollection<Order> Orders { get; private set; } = new List<Order>();
    public virtual ICollection<Review> Reviews { get; private set; } = new List<Review>();
    #endregion

    protected Customer()
    {
    }

    public Customer(User user, string fullName)
        : this(SavedUserId(user), fullName)
    {
        User = user;
        user.Customer = this;
    }

    public Customer(string userId, string fullName)
    {
        UserId = Guard.Required(userId, nameof(userId), FieldLengths.UserId);
        FullName = Guard.Required(fullName, nameof(fullName), FieldLengths.Name);
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    private static string SavedUserId(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        if (string.IsNullOrWhiteSpace(user.Id))
            throw new DomainException("Save the user account before creating a customer profile.");

        return user.Id;
    }

    public void Rename(string fullName)
    {
        FullName = Guard.Required(fullName, nameof(fullName), FieldLengths.Name);
        this.Touch();
    }

    public void Activate()
    {
        IsActive = true;
        this.Touch();
    }

    public void Deactivate()
    {
        IsActive = false;
        this.Touch();
    }

    public void AddLoyaltyPoints(int points)
    {
        LoyaltyPoints += Guard.Positive(points, nameof(points));
        this.Touch();
    }

    public void RedeemLoyaltyPoints(int points)
    {
        var amount = Guard.Positive(points, nameof(points));
        if (amount > LoyaltyPoints)
            throw new DomainException("Not enough loyalty points.");

        LoyaltyPoints -= amount;
        this.Touch();
    }

    public Address AddAddress(
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
        EnsureActive();
        var address = new Address(
            this, title, receiverName, receiverPhone, province, city, street, postalCode, plaque, unit);

        if (!Addresses.Any(a => !a.IsDeleted && a.IsDefault))
            address.MarkAsDefault();

        Addresses.Add(address);
        return address;
    }

    /// <summary>
    /// The cart navigation must already be loaded when this customer already has a cart.
    /// </summary>
    public CartItem AddToCart(Product product, int quantity)
    {
        EnsureActive();
        Cart ??= new Cart(this);
        return Cart.AddItem(product, quantity);
    }

    public IReadOnlyList<Order> ViewOrderHistory()
        => Orders.OrderByDescending(o => o.CreatedAt).ToList();

    internal void EnsureActive()
    {
        if (!IsActive)
            throw new DomainException("This customer account is not active.");
    }
}
