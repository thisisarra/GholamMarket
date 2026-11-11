using Application.Store;
using Domain.Common;
using Domain.Entities.Content;
using Domain.Entities.Customers;
using Domain.Entities.Sales;
using Domain.Entities.Users;
using Infrastructure.DBContexts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NibFramework.Commands;
using NibFramework.Model;
using NibFramework.Queries;
using Raya.Auth.Abstracations;
using Raya.Auth.Configuration;

namespace Infrastructure.Store;

public sealed class RegisterHandler(
    UserManager<User> users,
    SignInManager<User> signIn,
    GholamContext db) : ICommandHandler<RegisterCommand, Result<string>>
{
    public async Task<Result<string>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();
        var phone = request.Phone.Trim();
        var first = request.FirstName.Trim();
        var last = request.LastName.Trim();

        if (first.Length < 2 || last.Length < 2)
            return StoreSupport.Fail<string>("نام و نام خانوادگی را کامل وارد کنید.");
        if (!email.Contains('@', StringComparison.Ordinal))
            return StoreSupport.Fail<string>("ایمیل معتبر نیست.");
        if (phone.Length < 10)
            return StoreSupport.Fail<string>("شماره موبایل معتبر نیست.");

        var user = new User
        {
            UserName = email,
            Email = email,
            PhoneNumber = phone,
            FirstName = first,
            LastName = last,
            Status = 1,
            EmailConfirmed = true
        };

        var created = await users.CreateAsync(user, request.Password);
        if (!created.Succeeded)
            return StoreSupport.Fail<string>(IdentityText(created));

        await users.AddToRoleAsync(user, DefaultRoles.User);
        db.Customers.Add(new Customer(user, $"{first} {last}"));
        await db.SaveChangesAsync(cancellationToken);
        await signIn.SignInAsync(user, isPersistent: true);
        return StoreSupport.Ok("حساب کاربری ساخته شد.");
    }

    internal static string IdentityText(IdentityResult result)
        => string.Join(" ", result.Errors.Select(error => error.Code switch
        {
            "DuplicateEmail" or "DuplicateUserName" => "این ایمیل قبلا ثبت شده است.",
            "PasswordTooShort" => "رمز عبور باید حداقل ۶ کاراکتر باشد.",
            "PasswordRequiresDigit" => "رمز عبور باید عدد داشته باشد.",
            "PasswordRequiresUpper" => "رمز عبور باید حرف بزرگ انگلیسی داشته باشد.",
            "PasswordRequiresLower" => "رمز عبور باید حرف کوچک انگلیسی داشته باشد.",
            "PasswordRequiresNonAlphanumeric" => "رمز عبور باید یک نماد مثل @ داشته باشد.",
            _ => "اطلاعات ورود پذیرفته نشد."
        }));
}

public sealed class LoginHandler(
    UserManager<User> users,
    SignInManager<User> signIn,
    GholamContext db) : ICommandHandler<LoginCommand, Result<string>>
{
    public async Task<Result<string>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var identifier = request.Identifier.Trim();
        var user = identifier.Contains('@', StringComparison.Ordinal)
            ? await users.FindByEmailAsync(identifier)
            : await users.Users.FirstOrDefaultAsync(u => u.PhoneNumber == identifier, cancellationToken);

        if (user is null)
            return StoreSupport.Fail<string>("حسابی با این مشخصات پیدا نشد.");

        if (user.Status != 1)
            return StoreSupport.Fail<string>("این حساب غیرفعال است.");

        var customer = await db.Customers.FirstOrDefaultAsync(c => c.UserId == user.Id, cancellationToken);
        if (customer is { IsActive: false })
            return StoreSupport.Fail<string>("این حساب غیرفعال است.");

        var result = await signIn.PasswordSignInAsync(user, request.Password, isPersistent: true, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            return StoreSupport.Fail<string>(result.IsLockedOut
                ? "حساب به دلیل تلاش‌های ناموفق موقتا قفل شده است."
                : "ایمیل یا رمز عبور نادرست است.");
        }

        if (customer is null)
        {
            db.Customers.Add(new Customer(user, NameOf(user)));
            await db.SaveChangesAsync(cancellationToken);
        }

        return StoreSupport.Ok("وارد شدید.");
    }

    private static string NameOf(User user)
    {
        var name = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(name) ? (user.Email ?? "مشتری") : name;
    }
}

public sealed class LogoutHandler(SignInManager<User> signIn) : ICommandHandler<LogoutCommand, Result<string>>
{
    public async Task<Result<string>> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        await signIn.SignOutAsync();
        return StoreSupport.Ok("خارج شدید.");
    }
}

public sealed class GetDashboardHandler(GholamContext db, ICurrentUser current, UserManager<User> users)
    : IQueryHandler<GetDashboardQuery, Result<DashboardDto>>
{
    public async Task<Result<DashboardDto>> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        var customer = await db.Customers.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == current.UserId, cancellationToken);
        if (customer is null)
            return StoreSupport.Fail<DashboardDto>("پروفایل خرید پیدا نشد.");

        var user = await users.FindByIdAsync(current.UserId!);
        var orders = await db.Orders.AsNoTracking()
            .Include(o => o.Payments)
            .Include(o => o.Customer)
            .Where(o => o.CustomerId == customer.Id)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

        return Result<DashboardDto>.Success(new DashboardDto(
            customer.FullName,
            user?.Email,
            orders.Count,
            orders.Count(o => o.Status == OrderStatus.Pending),
            orders.Count(o => o.Status is OrderStatus.Paid or OrderStatus.Processing),
            orders.Take(5).Select(StoreSupport.ToSummary).ToList()));
    }
}

public sealed class GetProfileHandler(UserManager<User> users, ICurrentUser current)
    : IQueryHandler<GetProfileQuery, Result<ProfileDto>>
{
    public async Task<Result<ProfileDto>> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(current.UserId ?? "");
        if (user is null)
            return StoreSupport.Fail<ProfileDto>("کاربر پیدا نشد.");

        return Result<ProfileDto>.Success(new ProfileDto(
            user.FirstName ?? "",
            user.LastName ?? "",
            user.Email,
            user.PhoneNumber));
    }
}

public sealed class UpdateProfileHandler(UserManager<User> users, GholamContext db, ICurrentUser current)
    : ICommandHandler<UpdateProfileCommand, Result<string>>
{
    public async Task<Result<string>> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(current.UserId ?? "");
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.UserId == current.UserId, cancellationToken);
        if (user is null || customer is null)
            return StoreSupport.Fail<string>("پروفایل پیدا نشد.");

        var email = request.Email.Trim();
        if (!email.Contains('@', StringComparison.Ordinal))
            return StoreSupport.Fail<string>("ایمیل معتبر نیست.");

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        customer.Rename($"{user.FirstName} {user.LastName}");

        var emailResult = await users.SetEmailAsync(user, email);
        if (!emailResult.Succeeded)
            return StoreSupport.Fail<string>(RegisterHandler.IdentityText(emailResult));

        var nameResult = await users.SetUserNameAsync(user, email);
        if (!nameResult.Succeeded)
            return StoreSupport.Fail<string>(RegisterHandler.IdentityText(nameResult));

        var phoneResult = await users.SetPhoneNumberAsync(user, request.Phone.Trim());
        if (!phoneResult.Succeeded)
            return StoreSupport.Fail<string>("شماره موبایل پذیرفته نشد.");

        await db.SaveChangesAsync(cancellationToken);
        return StoreSupport.Ok("پروفایل ذخیره شد.");
    }
}

public sealed class UpdatePasswordHandler(UserManager<User> users, ICurrentUser current)
    : ICommandHandler<UpdatePasswordCommand, Result<string>>
{
    public async Task<Result<string>> Handle(UpdatePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(current.UserId ?? "");
        if (user is null)
            return StoreSupport.Fail<string>("کاربر پیدا نشد.");

        var result = await users.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        return result.Succeeded
            ? StoreSupport.Ok("رمز عبور تغییر کرد.")
            : StoreSupport.Fail<string>(RegisterHandler.IdentityText(result));
    }
}

public sealed class GetAddressesHandler(GholamContext db, ICurrentUser current)
    : IQueryHandler<GetAddressesQuery, Result<IReadOnlyList<AddressDto>>>
{
    public async Task<Result<IReadOnlyList<AddressDto>>> Handle(GetAddressesQuery request, CancellationToken cancellationToken)
    {
        var customer = await StoreSupport.LoadCustomerAsync(db, current.UserId, cancellationToken, addresses: true);
        if (customer is null)
            return StoreSupport.Fail<IReadOnlyList<AddressDto>>("پروفایل پیدا نشد.");

        var items = customer.Addresses.Where(a => !a.IsDeleted).Select(StoreSupport.ToAddress).ToList();
        return Result<IReadOnlyList<AddressDto>>.Success(items);
    }
}

public sealed class SaveAddressHandler(GholamContext db, ICurrentUser current)
    : ICommandHandler<SaveAddressCommand, Result<string>>
{
    public async Task<Result<string>> Handle(SaveAddressCommand request, CancellationToken cancellationToken)
    {
        var customer = await StoreSupport.LoadCustomerAsync(db, current.UserId, cancellationToken, addresses: true);
        if (customer is null)
            return StoreSupport.Fail<string>("پروفایل پیدا نشد.");

        try
        {
            Address address;
            if (request.Id is null or 0)
            {
                address = customer.AddAddress(
                    request.Title, request.ReceiverName, request.ReceiverPhone,
                    request.Province, request.City, request.Street, request.PostalCode,
                    request.Plaque, request.Unit);
            }
            else
            {
                address = customer.Addresses.FirstOrDefault(a => a.Id == request.Id && !a.IsDeleted)
                    ?? throw new DomainException("Address was not found.");
                address.Update(
                    request.Title, request.ReceiverName, request.ReceiverPhone,
                    request.Province, request.City, request.Street, request.PostalCode,
                    request.Plaque, request.Unit);
            }

            if (request.IsDefault)
                address.MarkAsDefault();

            await db.SaveChangesAsync(cancellationToken);
            return StoreSupport.Ok("آدرس ذخیره شد.");
        }
        catch (Exception ex) when (ex is DomainException or ArgumentException)
        {
            return StoreSupport.Fail<string>(ex is ArgumentException ? "اطلاعات آدرس کامل نیست." : StoreSupport.Persian(ex));
        }
    }
}

public sealed class DeleteAddressHandler(GholamContext db, ICurrentUser current)
    : ICommandHandler<DeleteAddressCommand, Result<string>>
{
    public async Task<Result<string>> Handle(DeleteAddressCommand request, CancellationToken cancellationToken)
    {
        var customer = await StoreSupport.LoadCustomerAsync(db, current.UserId, cancellationToken, addresses: true);
        var address = customer?.Addresses.FirstOrDefault(a => a.Id == request.Id && !a.IsDeleted);
        if (address is null)
            return StoreSupport.Fail<string>("آدرس پیدا نشد.");

        address.Delete();
        await db.SaveChangesAsync(cancellationToken);
        return StoreSupport.Ok("آدرس حذف شد.");
    }
}

public sealed class SetDefaultAddressHandler(GholamContext db, ICurrentUser current)
    : ICommandHandler<SetDefaultAddressCommand, Result<string>>
{
    public async Task<Result<string>> Handle(SetDefaultAddressCommand request, CancellationToken cancellationToken)
    {
        var customer = await StoreSupport.LoadCustomerAsync(db, current.UserId, cancellationToken, addresses: true);
        var address = customer?.Addresses.FirstOrDefault(a => a.Id == request.Id && !a.IsDeleted);
        if (address is null)
            return StoreSupport.Fail<string>("آدرس پیدا نشد.");

        address.MarkAsDefault();
        await db.SaveChangesAsync(cancellationToken);
        return StoreSupport.Ok("آدرس پیش‌فرض شد.");
    }
}

public sealed class GetMyOrdersHandler(GholamContext db, ICurrentUser current)
    : IQueryHandler<GetMyOrdersQuery, Result<IReadOnlyList<OrderSummaryDto>>>
{
    public async Task<Result<IReadOnlyList<OrderSummaryDto>>> Handle(GetMyOrdersQuery request, CancellationToken cancellationToken)
    {
        var customer = await db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.UserId == current.UserId, cancellationToken);
        if (customer is null)
            return StoreSupport.Fail<IReadOnlyList<OrderSummaryDto>>("پروفایل پیدا نشد.");

        var orders = await db.Orders
            .AsNoTracking()
            .Include(o => o.Payments)
            .Include(o => o.Customer)
            .Where(o => o.CustomerId == customer.Id)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<OrderSummaryDto>>.Success(orders.Select(StoreSupport.ToSummary).ToList());
    }
}

public sealed class GetOrderHandler(GholamContext db, ICurrentUser current)
    : IQueryHandler<GetOrderQuery, Result<OrderDetailDto>>
{
    public async Task<Result<OrderDetailDto>> Handle(GetOrderQuery request, CancellationToken cancellationToken)
    {
        var order = await db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.Payments)
            .Include(o => o.Shipment)
            .Include(o => o.Customer)
            .ThenInclude(c => c.User)
            .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);

        if (order is null || (!current.IsAdmin && order.Customer.UserId != current.UserId))
            return Result<OrderDetailDto>.NotFound("سفارش پیدا نشد.");

        return Result<OrderDetailDto>.Success(Map(order));
    }

    internal static OrderDetailDto Map(Order order)
    {
        var payment = order.Payments.OrderByDescending(p => p.Id).FirstOrDefault();
        return new OrderDetailDto(
            order.Id,
            order.OrderNumber,
            order.CreatedAt,
            order.Status,
            order.Subtotal,
            order.DiscountAmount,
            order.ShippingAmount,
            order.TotalAmount,
            order.CustomerNote,
            order.ReceiverName,
            order.ReceiverPhone,
            order.Province,
            order.City,
            order.Street,
            order.Plaque,
            order.PostalCode,
            order.Shipment?.TrackingCode,
            payment?.Status,
            order.Customer.FullName,
            order.Customer.User?.Email,
            order.Items.Select(i => new OrderLineDto(i.ProductName, i.Sku, i.Quantity, i.PriceAtPurchase, i.LineTotal)).ToList());
    }
}

public sealed class CancelOrderHandler(GholamContext db, ICurrentUser current)
    : ICommandHandler<CancelOrderCommand, Result<string>>
{
    public async Task<Result<string>> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Include(o => o.Payments)
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);

        if (order is null || (!current.IsAdmin && order.Customer.UserId != current.UserId))
            return StoreSupport.Fail<string>("سفارش پیدا نشد.");

        if (!current.IsAdmin && order.Status != OrderStatus.Pending)
            return StoreSupport.Fail<string>("فقط سفارش در انتظار پرداخت را می‌توانید لغو کنید.");

        try
        {
            order.Cancel();
            await db.SaveChangesAsync(cancellationToken);
            return StoreSupport.Ok("سفارش لغو شد و موجودی کالا برگردانده شد.");
        }
        catch (Exception ex) when (ex is DomainException or ArgumentException)
        {
            return StoreSupport.Fail<string>(StoreSupport.Persian(ex));
        }
    }
}

public sealed class SubmitContactHandler(GholamContext db)
    : ICommandHandler<SubmitContactCommand, Result<string>>
{
    public async Task<Result<string>> Handle(SubmitContactCommand request, CancellationToken cancellationToken)
    {
        try
        {
            db.ContactMessages.Add(new ContactMessage(request.Name, request.Email, request.Message));
            await db.SaveChangesAsync(cancellationToken);
            return StoreSupport.Ok("پیام شما ثبت شد.");
        }
        catch (ArgumentException)
        {
            return StoreSupport.Fail<string>("نام، ایمیل و متن پیام را کامل وارد کنید.");
        }
    }
}
