using Application.Store;
using Domain.Common;
using Domain.Entities.Catalog;
using Domain.Entities.Customers;
using Domain.Entities.Sales;
using Domain.Entities.Users;
using Infrastructure.DBContexts;
using Microsoft.EntityFrameworkCore;
using NibFramework.Model;
using Raya.Auth.Abstracations;

namespace Infrastructure.Store;

internal static class StoreSupport
{
    public const int StorePageSize = 12;
    public const int AdminPageSize = 15;

    public static async Task<Customer?> LoadCustomerAsync(
        GholamContext db,
        string? userId,
        CancellationToken cancellationToken,
        bool cart = false,
        bool addresses = false,
        bool orders = false)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return null;

        IQueryable<Customer> query = db.Customers.AsSplitQuery();
        if (cart)
        {
            query = query
                .Include(c => c.Cart!)
                .ThenInclude(c => c.Items)
                .ThenInclude(i => i.Product);
        }

        if (addresses)
            query = query.Include(c => c.Addresses);

        if (orders)
            query = query.Include(c => c.Orders);

        return await query.FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
    }

    public static Result<T> Fail<T>(string message) => Result<T>.Fail(message: message);

    public static Result<string> Ok(string message) => Result<string>.Success(message, message);

    public static string Persian(Exception exception)
    {
        var message = exception.Message;
        if (message.Contains("Not enough stock", StringComparison.OrdinalIgnoreCase))
            return "موجودی کالا برای این تعداد کافی نیست.";
        if (message.Contains("price", StringComparison.OrdinalIgnoreCase))
            return "قیمت کالا تغییر کرده است. سبد خرید را دوباره بررسی کنید.";
        if (message.Contains("not available", StringComparison.OrdinalIgnoreCase))
            return "این کالا در حال حاضر قابل فروش نیست.";
        if (message.Contains("empty cart", StringComparison.OrdinalIgnoreCase))
            return "سبد خرید خالی است.";
        if (message.Contains("does not belong", StringComparison.OrdinalIgnoreCase))
            return "این مورد به حساب شما تعلق ندارد.";
        if (message.Contains("deleted address", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("ship to a deleted", StringComparison.OrdinalIgnoreCase))
            return "این آدرس قابل استفاده نیست.";
        if (message.Contains("Cannot change order status", StringComparison.OrdinalIgnoreCase))
            return "تغییر وضعیت سفارش از حالت فعلی مجاز نیست.";
        if (message.Contains("Refund the successful", StringComparison.OrdinalIgnoreCase))
            return "سفارش پرداخت‌شده را باید از مسیر بازگشت وجه لغو کنید.";
        if (message.Contains("not active", StringComparison.OrdinalIgnoreCase))
            return "حساب کاربری غیرفعال است.";
        if (message.Contains("Discount code", StringComparison.OrdinalIgnoreCase))
            return "کد تخفیف قابل استفاده نیست.";
        if (message.Contains("already has an open", StringComparison.OrdinalIgnoreCase))
            return "برای این سفارش یک پرداخت باز وجود دارد.";

        return "امکان انجام این عملیات وجود ندارد.";
    }

    public static async Task<Result<T>?> RequireCustomerAsync<T>(
        ICurrentUser current,
        GholamContext db,
        CancellationToken cancellationToken,
        bool cart,
        bool addresses,
        bool orders,
        Action<Customer>? found = null)
    {
        if (!current.IsAuthenticated || string.IsNullOrWhiteSpace(current.UserId))
            return Fail<T>("برای ادامه وارد حساب کاربری شوید.");

        var customer = await LoadCustomerAsync(
            db, current.UserId, cancellationToken, cart, addresses, orders);

        if (customer is null)
            return Fail<T>("پروفایل خرید شما هنوز ساخته نشده است.");

        if (!customer.IsActive)
            return Fail<T>("حساب کاربری شما غیرفعال است.");

        found?.Invoke(customer);
        return null;
    }

    public static ProductCardDto ToCard(Product product)
        => new(
            product.Id,
            product.Name,
            product.Slug,
            product.ImageUrl,
            product.Price,
            product.CalculateDiscountPrice(),
            product.DiscountPercent,
            product.StockQuantity,
            product.Category?.Name ?? "");

    public static AddressDto ToAddress(Address address)
        => new(
            address.Id,
            address.Title,
            address.ReceiverName,
            address.ReceiverPhone,
            address.Province,
            address.City,
            address.Street,
            address.Plaque,
            address.Unit,
            address.PostalCode,
            address.IsDefault);

    public static CartDto ToCart(Cart? cart, bool priceChanged = false)
    {
        if (cart is null)
            return CartDto.Empty;

        var lines = cart.Items.Select(item =>
        {
            var product = item.Product;
            return new CartLineDto(
                item.ProductId,
                product?.Name ?? "کالا",
                product?.Slug ?? "",
                product?.ImageUrl,
                item.UnitPrice,
                product?.Price ?? item.UnitPrice,
                item.Quantity,
                product?.StockQuantity ?? 0,
                product?.IsActive ?? false,
                item.LineTotal);
        }).ToList();

        return new CartDto(lines, lines.Sum(x => x.LineTotal), lines.Sum(x => x.Quantity), priceChanged);
    }

    public static OrderSummaryDto ToSummary(Order order)
    {
        var payment = order.Payments.OrderByDescending(p => p.Id).FirstOrDefault();
        return new OrderSummaryDto(
            order.Id,
            order.OrderNumber,
            order.CreatedAt,
            order.TotalAmount,
            order.Status,
            payment?.Status,
            order.Customer?.FullName ?? "");
    }

    public static string Slugify(string value)
    {
        var slug = value.Trim().ToLowerInvariant().Replace(' ', '-');
        while (slug.Contains("--", StringComparison.Ordinal))
            slug = slug.Replace("--", "-", StringComparison.Ordinal);

        return slug.Trim('-');
    }
}
