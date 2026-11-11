using Domain.Common;
using NibFramework.Model;

namespace Domain.Entities.Sales;

public enum PaymentMethod
{
    OnlineGateway = 1,
    Card = 2,
    CashOnDelivery = 3,
    Wallet = 4
}

public enum PaymentStatus
{
    Pending = 1,
    Succeeded = 2,
    Failed = 3,
    Refunded = 4
}

public class Payment : BaseEntity<int>
{
    public int OrderId { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentMethod Method { get; private set; }
    public PaymentStatus Status { get; private set; }
    public string? Reference { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime? PaidAt { get; private set; }

    public virtual Order Order { get; private set; } = null!;

    protected Payment()
    {
    }

    internal Payment(Order order, decimal amount, PaymentMethod method)
    {
        Order = order;
        if (order.Id != 0)
            OrderId = order.Id;

        Amount = Guard.Money(amount, nameof(amount));
        Method = method;
        Status = PaymentStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public void Process(bool succeeded, string? reference = null, string? failureReason = null)
    {
        if (succeeded)
            MarkSucceeded(reference ?? throw new ArgumentException("A reference is required when the payment succeeds.", nameof(reference)));
        else
            MarkFailed(failureReason ?? "Payment failed.");
    }

    public void MarkSucceeded(string reference)
    {
        if (Status != PaymentStatus.Pending)
            throw new DomainException("Only a pending payment can be completed.");

        if (Order.Status != OrderStatus.Pending)
            throw new DomainException("Only a pending order can be marked as paid.");

        Reference = Guard.Required(reference, nameof(reference), FieldLengths.Reference);
        Status = PaymentStatus.Succeeded;
        PaidAt = DateTime.UtcNow;
        this.Touch();
        Order.UpdateStatus(OrderStatus.Paid);
    }

    public void MarkFailed(string reason)
    {
        if (Status != PaymentStatus.Pending)
            throw new DomainException("Only a pending payment can fail.");

        FailureReason = Guard.Required(reason, nameof(reason), FieldLengths.Reason);
        Status = PaymentStatus.Failed;
        this.Touch();
    }

    public void Refund()
    {
        if (Status != PaymentStatus.Succeeded)
            throw new DomainException("Only a successful payment can be refunded.");

        Order.UpdateStatus(OrderStatus.Refunded);
        Status = PaymentStatus.Refunded;
        this.Touch();
    }
}
