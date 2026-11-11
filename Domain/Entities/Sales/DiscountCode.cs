using Domain.Common;
using NibFramework.Model;

namespace Domain.Entities.Sales;

public enum DiscountType
{
    Percent = 1,
    FixedAmount = 2
}

public class DiscountCode : BaseEntity<int>
{
    public string Code { get; private set; } = null!;
    public DiscountType Type { get; private set; }
    public decimal DiscountPercent { get; private set; }
    public decimal FixedAmount { get; private set; }
    public decimal? MinOrderAmount { get; private set; }
    public decimal? MaxDiscountAmount { get; private set; }
    public DateTime? StartsAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public int? UsageLimit { get; private set; }
    public int UsedCount { get; private set; }
    public bool IsActive { get; private set; }

    public virtual ICollection<Order> Orders { get; private set; } = new List<Order>();

    protected DiscountCode()
    {
    }

    public DiscountCode(
        string code,
        DiscountType type,
        decimal value,
        DateTime? startsAt = null,
        DateTime? expiresAt = null,
        decimal? minOrderAmount = null,
        decimal? maxDiscountAmount = null,
        int? usageLimit = null)
    {
        Code = Guard.Required(code, nameof(code), FieldLengths.Code).ToUpperInvariant();
        Type = type;

        if (startsAt is not null && expiresAt is not null && expiresAt <= startsAt)
            throw new ArgumentException("Expiry must be later than the start.", nameof(expiresAt));

        if (type == DiscountType.Percent)
        {
            if (value <= 0 || value > 100)
                throw new ArgumentOutOfRangeException(nameof(value), "Percent discount must be greater than 0 and at most 100.");
            DiscountPercent = decimal.Round(value, 2, MidpointRounding.AwayFromZero);
        }
        else
        {
            FixedAmount = Guard.Money(value, nameof(value));
            if (FixedAmount == 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Fixed discount must be greater than zero.");
        }

        MinOrderAmount = minOrderAmount is null ? null : Guard.Money(minOrderAmount.Value, nameof(minOrderAmount));
        MaxDiscountAmount = maxDiscountAmount is null ? null : Guard.Money(maxDiscountAmount.Value, nameof(maxDiscountAmount));
        if (usageLimit is <= 0)
            throw new ArgumentOutOfRangeException(nameof(usageLimit), "Usage limit must be greater than zero.");

        UsageLimit = usageLimit;
        StartsAt = startsAt;
        ExpiresAt = expiresAt;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Returns null when the code can be applied. Otherwise returns the reason it cannot.
    /// </summary>
    public string? Validate(decimal subtotal, DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        if (!IsActive)
            return "Discount code is not active.";
        if (StartsAt is not null && now < StartsAt)
            return "Discount code is not active yet.";
        if (ExpiresAt is not null && now > ExpiresAt)
            return "Discount code has expired.";
        if (UsageLimit is not null && UsedCount >= UsageLimit)
            return "Discount code has reached its usage limit.";
        if (subtotal <= 0)
            return "Discount code cannot be applied to an empty order.";
        if (MinOrderAmount is not null && subtotal < MinOrderAmount)
            return $"Order subtotal must be at least {MinOrderAmount:0.##} to use this code.";

        return null;
    }

    public decimal Calculate(decimal subtotal)
    {
        var discount = Type == DiscountType.Percent
            ? decimal.Round(subtotal * DiscountPercent / 100m, 2, MidpointRounding.AwayFromZero)
            : FixedAmount;

        if (MaxDiscountAmount is { } cap && discount > cap)
            discount = cap;

        if (discount > subtotal)
            discount = subtotal;

        return discount;
    }

    public decimal ApplyTo(decimal subtotal, DateTime? utcNow = null)
    {
        var error = Validate(subtotal, utcNow);
        if (error is not null)
            throw new DomainException(error);

        UsedCount++;
        this.Touch();
        return Calculate(subtotal);
    }

    public void Deactivate()
    {
        IsActive = false;
        this.Touch();
    }
}
