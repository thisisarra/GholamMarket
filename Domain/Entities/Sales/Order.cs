using Domain.Common;
using Domain.Entities.Catalog;
using Domain.Entities.Customers;
using NibFramework.Model;

namespace Domain.Entities.Sales;

public enum OrderStatus
{
    Pending = 1,
    Paid = 2,
    Processing = 3,
    Shipped = 4,
    Delivered = 5,
    Cancelled = 6,
    Refunded = 7
}

public class Order : BaseEntity<int>
{
    private static readonly Dictionary<OrderStatus, OrderStatus[]> AllowedTransitions = new()
    {
        [OrderStatus.Pending] = [OrderStatus.Paid, OrderStatus.Processing, OrderStatus.Cancelled],
        [OrderStatus.Paid] = [OrderStatus.Processing, OrderStatus.Cancelled, OrderStatus.Refunded],
        [OrderStatus.Processing] = [OrderStatus.Shipped, OrderStatus.Cancelled, OrderStatus.Refunded],
        [OrderStatus.Shipped] = [OrderStatus.Delivered, OrderStatus.Refunded],
        [OrderStatus.Delivered] = [OrderStatus.Refunded],
        [OrderStatus.Cancelled] = [],
        [OrderStatus.Refunded] = []
    };

    public string OrderNumber { get; private set; } = null!;
    public int CustomerId { get; private set; }
    public int? DiscountCodeId { get; private set; }
    public int? ShippingAddressId { get; private set; }
    public OrderStatus Status { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal ShippingAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string? CustomerNote { get; private set; }
    public DateTime? PaidAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }

    #region Shipping snapshot
    public string ReceiverName { get; private set; } = null!;
    public string ReceiverPhone { get; private set; } = null!;
    public string Province { get; private set; } = null!;
    public string City { get; private set; } = null!;
    public string Street { get; private set; } = null!;
    public string? Plaque { get; private set; }
    public string? Unit { get; private set; }
    public string PostalCode { get; private set; } = null!;
    #endregion

    #region Relations
    public virtual Customer Customer { get; private set; } = null!;
    public virtual Address? ShippingAddress { get; private set; }
    public virtual DiscountCode? DiscountCode { get; private set; }
    public virtual Shipment? Shipment { get; private set; }
    public virtual ICollection<OrderItem> Items { get; private set; } = new List<OrderItem>();
    public virtual ICollection<Payment> Payments { get; private set; } = new List<Payment>();
    #endregion

    protected Order()
    {
    }

    public static Order Place(
        Customer customer,
        Cart cart,
        Address shippingAddress,
        decimal shippingAmount,
        DiscountCode? discountCode = null)
    {
        ArgumentNullException.ThrowIfNull(customer);
        ArgumentNullException.ThrowIfNull(cart);
        ArgumentNullException.ThrowIfNull(shippingAddress);
        customer.EnsureActive();
        EnsureOwned(customer, cart.Customer, cart.CustomerId, "Cart");
        EnsureOwned(customer, shippingAddress.Customer, shippingAddress.CustomerId, "Shipping address");

        if (shippingAddress.IsDeleted)
            throw new DomainException("Cannot ship to a deleted address.");
        if (cart.Items.Count == 0)
            throw new DomainException("Cannot place an order from an empty cart.");

        var lines = cart.Items.Select(item =>
        {
            var product = item.Product ?? throw new DomainException("Cart items must include their products.");
            if (!product.IsActive)
                throw new DomainException($"'{product.Name}' is not available.");

            var price = product.CalculateDiscountPrice();
            if (item.UnitPrice != price)
                throw new DomainException($"The price of '{product.Name}' has changed. Refresh the cart and try again.");

            return (item, product, price);
        }).ToList();

        foreach (var group in lines.GroupBy(x => x.product))
        {
            var quantity = group.Sum(x => x.item.Quantity);
            if (quantity > group.Key.StockQuantity)
                throw new DomainException($"Not enough stock for '{group.Key.Name}'.");
        }

        var subtotal = lines.Sum(x => decimal.Round(x.price * x.item.Quantity, 2, MidpointRounding.AwayFromZero));
        decimal discount = 0;
        if (discountCode is not null)
            discount = discountCode.ApplyTo(subtotal);

        var order = new Order(customer, shippingAddress, shippingAmount);
        foreach (var line in lines)
        {
            line.product.DecreaseStock(line.item.Quantity);
            order.Items.Add(new OrderItem(order, line.product, line.item.Quantity, line.price));
        }

        order.DiscountAmount = discount;
        if (discountCode is not null)
        {
            order.DiscountCode = discountCode;
            if (discountCode.Id != 0)
                order.DiscountCodeId = discountCode.Id;
        }

        order.Recalculate();
        cart.Clear();
        customer.Orders.Add(order);
        return order;
    }

    public decimal CalculateTotal()
    {
        Recalculate();
        this.Touch();
        return TotalAmount;
    }

    public void SetNote(string? note)
    {
        CustomerNote = Guard.Optional(note, nameof(note), FieldLengths.Comment);
        this.Touch();
    }

    public void UpdateStatus(OrderStatus status)
    {
        if (Status == status)
            return;

        if (!AllowedTransitions[Status].Contains(status))
            throw new DomainException($"Cannot change order status from {Status} to {status}.");

        if (status == OrderStatus.Cancelled)
        {
            if (Payments.Any(p => p.Status == PaymentStatus.Succeeded))
                throw new DomainException("Refund the successful payment instead of cancelling this order.");

            Restock();
            foreach (var payment in Payments.Where(p => p.Status == PaymentStatus.Pending).ToList())
                payment.MarkFailed("Order was cancelled.");
        }

        Status = status;
        if (status == OrderStatus.Cancelled)
            CancelledAt = DateTime.UtcNow;
        if (status == OrderStatus.Paid)
            PaidAt ??= DateTime.UtcNow;

        this.Touch();
    }

    public void Cancel() => UpdateStatus(OrderStatus.Cancelled);

    public Payment StartPayment(PaymentMethod method)
    {
        if (Status != OrderStatus.Pending)
            throw new DomainException("Only a pending order can start a payment.");

        if (Payments.Any(p => p.Status is PaymentStatus.Pending or PaymentStatus.Succeeded))
            throw new DomainException("This order already has an open or completed payment.");

        var payment = new Payment(this, TotalAmount, method);
        Payments.Add(payment);
        return payment;
    }

    public Shipment CreateShipment(string? carrier = null)
    {
        if (Shipment is not null)
            throw new DomainException("This order already has a shipment.");

        if (Status is not (OrderStatus.Paid or OrderStatus.Processing))
            throw new DomainException("A shipment can be created only for a paid or processing order.");

        if (Status == OrderStatus.Paid)
            UpdateStatus(OrderStatus.Processing);

        Shipment = new Shipment(this, carrier);
        return Shipment;
    }

    private Order(Customer customer, Address address, decimal shippingAmount)
    {
        Customer = customer;
        if (customer.Id != 0)
            CustomerId = customer.Id;

        ShippingAddress = address;
        if (address.Id != 0)
            ShippingAddressId = address.Id;

        OrderNumber = $"GM-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
        Status = OrderStatus.Pending;
        ReceiverName = address.ReceiverName;
        ReceiverPhone = address.ReceiverPhone;
        Province = address.Province;
        City = address.City;
        Street = address.Street;
        Plaque = address.Plaque;
        Unit = address.Unit;
        PostalCode = address.PostalCode;
        ShippingAmount = Guard.Money(shippingAmount, nameof(shippingAmount));
        CreatedAt = DateTime.UtcNow;
    }

    private void Recalculate()
    {
        Subtotal = Items.Sum(i => i.LineTotal);
        var total = Subtotal - DiscountAmount + ShippingAmount;
        TotalAmount = total < 0 ? 0 : decimal.Round(total, 2, MidpointRounding.AwayFromZero);
    }

    private void Restock()
    {
        if (Items.Any(i => i.Product is null))
            throw new DomainException("Load order items with their products before cancelling so stock can be restored.");

        foreach (var item in Items)
            item.Product.IncreaseStock(item.Quantity);
    }

    private static void EnsureOwned(Customer customer, Customer? navigation, int ownerId, string subject)
    {
        var owned = ReferenceEquals(navigation, customer) ||
                    (customer.Id != 0 && ownerId == customer.Id);
        if (!owned)
            throw new DomainException($"{subject} does not belong to this customer.");
    }
}
