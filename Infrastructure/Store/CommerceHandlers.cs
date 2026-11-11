using Application.Store;
using Domain.Common;
using Domain.Entities.Sales;
using Infrastructure.DBContexts;
using Microsoft.EntityFrameworkCore;
using NibFramework.Commands;
using NibFramework.Model;
using NibFramework.Queries;
using Raya.Auth.Abstracations;

namespace Infrastructure.Store;

public sealed class GetCartHandler(GholamContext db, ICurrentUser current)
    : IQueryHandler<GetCartQuery, Result<CartDto>>
{
    public async Task<Result<CartDto>> Handle(GetCartQuery request, CancellationToken cancellationToken)
    {
        if (!current.IsAuthenticated)
            return Result<CartDto>.Success(CartDto.Empty);

        var customer = await StoreSupport.LoadCustomerAsync(db, current.UserId, cancellationToken, cart: true);
        return Result<CartDto>.Success(StoreSupport.ToCart(customer?.Cart));
    }
}

public sealed class AddToCartHandler(GholamContext db, ICurrentUser current)
    : ICommandHandler<AddToCartCommand, Result<string>>
{
    public async Task<Result<string>> Handle(AddToCartCommand request, CancellationToken cancellationToken)
    {
        CustomerBox box = default;
        var error = await StoreSupport.RequireCustomerAsync<string>(
            current, db, cancellationToken, cart: true, addresses: false, orders: false, found: c => box = new(c));
        if (error is not null)
            return error;

        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);
        if (product is null)
            return StoreSupport.Fail<string>("کالا پیدا نشد.");

        try
        {
            box.Customer!.AddToCart(product, request.Quantity);
            await db.SaveChangesAsync(cancellationToken);
            return StoreSupport.Ok("کالا به سبد خرید اضافه شد.");
        }
        catch (Exception ex) when (ex is DomainException or ArgumentException)
        {
            return StoreSupport.Fail<string>(StoreSupport.Persian(ex));
        }
    }

    private readonly record struct CustomerBox(Domain.Entities.Customers.Customer? Customer);
}

public sealed class UpdateCartItemHandler(GholamContext db, ICurrentUser current)
    : ICommandHandler<UpdateCartItemCommand, Result<string>>
{
    public async Task<Result<string>> Handle(UpdateCartItemCommand request, CancellationToken cancellationToken)
    {
        var customer = await StoreSupport.LoadCustomerAsync(db, current.UserId, cancellationToken, cart: true);
        if (customer?.Cart is null)
            return StoreSupport.Fail<string>("سبد خرید پیدا نشد.");

        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);
        if (product is null)
            return StoreSupport.Fail<string>("کالا پیدا نشد.");

        try
        {
            customer.Cart.ChangeItemQuantity(product, request.Quantity);
            await db.SaveChangesAsync(cancellationToken);
            return StoreSupport.Ok("تعداد کالا به‌روزرسانی شد.");
        }
        catch (Exception ex) when (ex is DomainException or ArgumentException)
        {
            return StoreSupport.Fail<string>(StoreSupport.Persian(ex));
        }
    }
}

public sealed class RemoveCartItemHandler(GholamContext db, ICurrentUser current)
    : ICommandHandler<RemoveCartItemCommand, Result<string>>
{
    public async Task<Result<string>> Handle(RemoveCartItemCommand request, CancellationToken cancellationToken)
    {
        var customer = await StoreSupport.LoadCustomerAsync(db, current.UserId, cancellationToken, cart: true);
        var product = customer?.Cart?.Items.Select(i => i.Product).FirstOrDefault(p => p != null && p.Id == request.ProductId)
            ?? await db.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        if (customer?.Cart is null || product is null)
            return StoreSupport.Fail<string>("این کالا در سبد نیست.");

        try
        {
            customer.Cart.RemoveItem(product);
            await db.SaveChangesAsync(cancellationToken);
            return StoreSupport.Ok("کالا از سبد حذف شد.");
        }
        catch (Exception ex) when (ex is DomainException or ArgumentException)
        {
            return StoreSupport.Fail<string>(StoreSupport.Persian(ex));
        }
    }
}

public sealed class GetCheckoutHandler(GholamContext db, ICurrentUser current)
    : IQueryHandler<GetCheckoutQuery, Result<CheckoutDto>>
{
    public async Task<Result<CheckoutDto>> Handle(GetCheckoutQuery request, CancellationToken cancellationToken)
    {
        var customer = await StoreSupport.LoadCustomerAsync(
            db, current.UserId, cancellationToken, cart: true, addresses: true);
        if (customer is null)
            return StoreSupport.Fail<CheckoutDto>("برای تسویه حساب وارد شوید.");

        var changed = false;
        if (customer.Cart is not null)
        {
            var before = customer.Cart.Items.Select(i => (i.ProductId, i.UnitPrice)).ToList();
            try
            {
                customer.Cart.RefreshPrices();
            }
            catch (DomainException ex)
            {
                return StoreSupport.Fail<CheckoutDto>(StoreSupport.Persian(ex));
            }

            changed = customer.Cart.Items.Any(i => before.Any(b => b.ProductId == i.ProductId && b.UnitPrice != i.UnitPrice));
            if (changed)
                await db.SaveChangesAsync(cancellationToken);
        }

        var cart = StoreSupport.ToCart(customer.Cart, changed);
        var shipping = ShippingCost.For(cart.Subtotal);
        var addresses = customer.Addresses.Where(a => !a.IsDeleted).Select(StoreSupport.ToAddress).ToList();
        return Result<CheckoutDto>.Success(new CheckoutDto(cart, addresses, shipping, cart.Subtotal + shipping));
    }
}

public sealed class PlaceOrderHandler(GholamContext db, ICurrentUser current)
    : ICommandHandler<PlaceOrderCommand, Result<int>>
{
    public async Task<Result<int>> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
    {
        var customer = await StoreSupport.LoadCustomerAsync(
            db, current.UserId, cancellationToken, cart: true, addresses: true, orders: true);
        if (customer?.Cart is null)
            return StoreSupport.Fail<int>("سبد خرید پیدا نشد.");

        var address = customer.Addresses.FirstOrDefault(a => a.Id == request.AddressId && !a.IsDeleted);
        if (address is null)
            return StoreSupport.Fail<int>("آدرس تحویل را انتخاب کنید.");

        DiscountCode? coupon = null;
        if (!string.IsNullOrWhiteSpace(request.CouponCode))
        {
            var code = request.CouponCode.Trim().ToUpperInvariant();
            coupon = await db.DiscountCodes.FirstOrDefaultAsync(c => c.Code == code, cancellationToken);
            if (coupon is null)
                return StoreSupport.Fail<int>("کد تخفیف پیدا نشد.");
        }

        var shipping = ShippingCost.For(customer.Cart.GetTotal());
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var order = Order.Place(customer, customer.Cart, address, shipping, coupon);
            if (!string.IsNullOrWhiteSpace(request.Note))
                order.SetNote(request.Note);

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Result<int>.Success(order.Id, "سفارش ثبت شد.");
        }
        catch (DbUpdateConcurrencyException)
        {
            return StoreSupport.Fail<int>("موجودی کالا همزمان تغییر کرده است. لطفا دوباره تلاش کنید.");
        }
        catch (Exception ex) when (ex is DomainException or ArgumentException)
        {
            return StoreSupport.Fail<int>(StoreSupport.Persian(ex));
        }
    }
}

public sealed class GetPaymentHandler(GholamContext db, ICurrentUser current)
    : IQueryHandler<GetPaymentQuery, Result<PaymentDto>>
{
    public async Task<Result<PaymentDto>> Handle(GetPaymentQuery request, CancellationToken cancellationToken)
    {
        var order = await OwnedOrder(db, current, request.OrderId, cancellationToken);
        if (order is null)
            return StoreSupport.Fail<PaymentDto>("سفارش پیدا نشد.");

        if (order.Status != OrderStatus.Pending)
        {
            var latest = order.Payments.OrderByDescending(p => p.Id).FirstOrDefault();
            return Result<PaymentDto>.Success(new PaymentDto(
                latest?.Id ?? 0,
                order.Id,
                order.OrderNumber,
                order.TotalAmount,
                latest?.Status ?? PaymentStatus.Pending));
        }

        var payment = order.Payments.FirstOrDefault(p => p.Status == PaymentStatus.Pending);
        if (payment is null)
        {
            try
            {
                payment = order.StartPayment(PaymentMethod.OnlineGateway);
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex) when (ex is DomainException or ArgumentException)
            {
                return StoreSupport.Fail<PaymentDto>(StoreSupport.Persian(ex));
            }
        }

        return Result<PaymentDto>.Success(new PaymentDto(
            payment.Id, order.Id, order.OrderNumber, payment.Amount, payment.Status));
    }

    internal static async Task<Order?> OwnedOrder(GholamContext db, ICurrentUser current, int orderId, CancellationToken cancellationToken)
    {
        var order = await db.Orders
            .Include(o => o.Payments)
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order is null)
            return null;

        if (current.IsAdmin || order.Customer.UserId == current.UserId)
            return order;

        return null;
    }
}

public sealed class VerifyPaymentHandler(GholamContext db, ICurrentUser current, IPaymentGateway gateway)
    : ICommandHandler<VerifyPaymentCommand, Result<int>>
{
    public async Task<Result<int>> Handle(VerifyPaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await db.Payments
            .Include(p => p.Order)
            .ThenInclude(o => o.Customer)
            .FirstOrDefaultAsync(p => p.Id == request.PaymentId, cancellationToken);

        if (payment is null || (!current.IsAdmin && payment.Order.Customer.UserId != current.UserId))
            return StoreSupport.Fail<int>("پرداخت پیدا نشد.");

        if (payment.Status == PaymentStatus.Succeeded)
            return Result<int>.Success(payment.OrderId, "این پرداخت قبلا تایید شده است.");

        if (payment.Status != PaymentStatus.Pending)
            return StoreSupport.Fail<int>("این پرداخت دیگر قابل تایید نیست.");

        if (payment.Amount != payment.Order.TotalAmount)
            return StoreSupport.Fail<int>("مبلغ پرداخت با مبلغ سفارش یکسان نیست.");

        var verification = await gateway.VerifyAsync(
            new PaymentVerificationRequest(
                payment.Id,
                payment.Order.OrderNumber,
                payment.Amount,
                request.Approved,
                null),
            cancellationToken);

        try
        {
            payment.Process(verification.Succeeded, verification.Reference, verification.FailureReason);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DomainException ex) when (payment.Status == PaymentStatus.Succeeded)
        {
            return Result<int>.Success(payment.OrderId, StoreSupport.Persian(ex));
        }
        catch (Exception ex) when (ex is DomainException or ArgumentException)
        {
            return StoreSupport.Fail<int>(StoreSupport.Persian(ex));
        }

        return verification.Succeeded
            ? Result<int>.Success(payment.OrderId, "پرداخت با موفقیت تایید شد.")
            : Result<int>.Fail(payment.OrderId, verification.FailureReason ?? "پرداخت ناموفق بود.");
    }
}
