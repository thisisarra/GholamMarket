using Domain.Common;
using NibFramework.Model;

namespace Domain.Entities.Sales;

public enum ShipmentStatus
{
    Pending = 1,
    Packed = 2,
    Shipped = 3,
    InTransit = 4,
    Delivered = 5,
    Returned = 6
}

public class Shipment : BaseEntity<int>
{
    public int OrderId { get; private set; }
    public string? Carrier { get; private set; }
    public string? TrackingCode { get; private set; }
    public ShipmentStatus Status { get; private set; }
    public DateTime? ShippedAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }

    public virtual Order Order { get; private set; } = null!;

    protected Shipment()
    {
    }

    internal Shipment(Order order, string? carrier)
    {
        Order = order;
        if (order.Id != 0)
            OrderId = order.Id;

        Carrier = Guard.Optional(carrier, nameof(carrier), FieldLengths.Carrier);
        Status = ShipmentStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateTrackingStatus(ShipmentStatus status, string? trackingCode = null, string? carrier = null)
    {
        Status = status;
        if (!string.IsNullOrWhiteSpace(trackingCode))
            TrackingCode = Guard.Required(trackingCode, nameof(trackingCode), FieldLengths.TrackingCode);
        if (!string.IsNullOrWhiteSpace(carrier))
            Carrier = Guard.Required(carrier, nameof(carrier), FieldLengths.Carrier);

        var now = DateTime.UtcNow;
        if (status is ShipmentStatus.Shipped or ShipmentStatus.InTransit)
            ShippedAt ??= now;

        if (status == ShipmentStatus.Delivered)
        {
            ShippedAt ??= now;
            DeliveredAt = now;
        }

        this.Touch();
        AdvanceOrder(status);
    }

    private void AdvanceOrder(ShipmentStatus status)
    {
        if (status is ShipmentStatus.Shipped or ShipmentStatus.InTransit or ShipmentStatus.Delivered)
        {
            if (Order.Status == OrderStatus.Paid)
                Order.UpdateStatus(OrderStatus.Processing);
            if (Order.Status == OrderStatus.Processing)
                Order.UpdateStatus(OrderStatus.Shipped);
        }

        if (status == ShipmentStatus.Delivered && Order.Status == OrderStatus.Shipped)
            Order.UpdateStatus(OrderStatus.Delivered);
    }
}
