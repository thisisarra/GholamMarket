using System.Globalization;
using Domain.Entities.Sales;

namespace GholamMarket.Ui;

public static class ShopFormat
{
    private static readonly PersianCalendar Calendar = new();
    private static readonly CultureInfo Persian = CultureInfo.GetCultureInfo("fa-IR");

    public static string Money(decimal amount)
        => string.Create(Persian, $"{amount:N0}") + " تومان";

    public static string When(DateTime value)
    {
        var utc = value.Kind switch
        {
            DateTimeKind.Local => value.ToUniversalTime(),
            DateTimeKind.Utc => value,
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

        var local = utc.AddMinutes(210);
        return $"{Calendar.GetYear(local):0000}/{Calendar.GetMonth(local):00}/{Calendar.GetDayOfMonth(local):00} {local:HH:mm}";
    }

    public static string OrderStatus(OrderStatus status) => status switch
    {
        Domain.Entities.Sales.OrderStatus.Pending => "در انتظار پرداخت",
        Domain.Entities.Sales.OrderStatus.Paid => "پرداخت شده",
        Domain.Entities.Sales.OrderStatus.Processing => "در حال پردازش",
        Domain.Entities.Sales.OrderStatus.Shipped => "ارسال شده",
        Domain.Entities.Sales.OrderStatus.Delivered => "تحویل شده",
        Domain.Entities.Sales.OrderStatus.Cancelled => "لغو شده",
        Domain.Entities.Sales.OrderStatus.Refunded => "مسترد شده",
        _ => status.ToString()
    };

    public static string PaymentStatus(PaymentStatus? status) => status switch
    {
        Domain.Entities.Sales.PaymentStatus.Pending => "در انتظار",
        Domain.Entities.Sales.PaymentStatus.Succeeded => "موفق",
        Domain.Entities.Sales.PaymentStatus.Failed => "ناموفق",
        Domain.Entities.Sales.PaymentStatus.Refunded => "مسترد",
        _ => "ثبت نشده"
    };

    public static string Badge(OrderStatus status) => status switch
    {
        Domain.Entities.Sales.OrderStatus.Pending => "text-bg-warning",
        Domain.Entities.Sales.OrderStatus.Paid => "text-bg-success",
        Domain.Entities.Sales.OrderStatus.Processing => "text-bg-info",
        Domain.Entities.Sales.OrderStatus.Shipped => "text-bg-primary",
        Domain.Entities.Sales.OrderStatus.Delivered => "text-bg-success",
        Domain.Entities.Sales.OrderStatus.Cancelled => "text-bg-secondary",
        Domain.Entities.Sales.OrderStatus.Refunded => "text-bg-dark",
        _ => "text-bg-light"
    };

    public static IReadOnlyList<OrderStatus> NextStatuses(OrderStatus status) => status switch
    {
        Domain.Entities.Sales.OrderStatus.Pending => [Domain.Entities.Sales.OrderStatus.Processing, Domain.Entities.Sales.OrderStatus.Cancelled],
        Domain.Entities.Sales.OrderStatus.Paid => [Domain.Entities.Sales.OrderStatus.Processing, Domain.Entities.Sales.OrderStatus.Cancelled, Domain.Entities.Sales.OrderStatus.Refunded],
        Domain.Entities.Sales.OrderStatus.Processing => [Domain.Entities.Sales.OrderStatus.Shipped, Domain.Entities.Sales.OrderStatus.Cancelled, Domain.Entities.Sales.OrderStatus.Refunded],
        Domain.Entities.Sales.OrderStatus.Shipped => [Domain.Entities.Sales.OrderStatus.Delivered, Domain.Entities.Sales.OrderStatus.Refunded],
        Domain.Entities.Sales.OrderStatus.Delivered => [Domain.Entities.Sales.OrderStatus.Refunded],
        _ => []
    };
}
