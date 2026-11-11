using Application.Store;
using Domain.Common;
using Domain.Entities.Catalog;
using Domain.Entities.Sales;
using Domain.Entities.Users;
using Infrastructure.DBContexts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NibFramework.Commands;
using NibFramework.Model;
using NibFramework.Queries;
using Raya.Auth.Configuration;

namespace Infrastructure.Store;

public sealed class AdminDashboardHandler(GholamContext db, UserManager<User> users)
    : IQueryHandler<AdminDashboardQuery, Result<AdminDashboardDto>>
{
    public async Task<Result<AdminDashboardDto>> Handle(AdminDashboardQuery request, CancellationToken cancellationToken)
    {
        var orders = await db.Orders.AsNoTracking().Include(o => o.Payments).Include(o => o.Customer).ToListAsync(cancellationToken);
        var salesStatuses = new[] { OrderStatus.Paid, OrderStatus.Processing, OrderStatus.Shipped, OrderStatus.Delivered };

        var dto = new AdminDashboardDto(
            await users.Users.CountAsync(cancellationToken),
            await db.Products.CountAsync(cancellationToken),
            await db.Products.CountAsync(p => p.IsActive, cancellationToken),
            orders.Count,
            orders.Count(o => o.Status == OrderStatus.Pending),
            orders.Count(o => o.Status == OrderStatus.Paid),
            orders.Count(o => o.Status == OrderStatus.Processing),
            orders.Count(o => o.Status == OrderStatus.Delivered),
            orders.Where(o => salesStatuses.Contains(o.Status)).Sum(o => o.TotalAmount),
            orders.OrderByDescending(o => o.CreatedAt).Take(8).Select(StoreSupport.ToSummary).ToList());

        return Result<AdminDashboardDto>.Success(dto);
    }
}

public sealed class AdminProductsHandler(GholamContext db)
    : IQueryHandler<AdminProductsQuery, Result<PageDto<AdminProductDto>>>
{
    public async Task<Result<PageDto<AdminProductDto>>> Handle(AdminProductsQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var query = db.Products.AsNoTracking().Include(p => p.Category).AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(p => p.Name.Contains(term) || p.Sku.Contains(term));
        }

        if (request.CategoryId is > 0)
            query = query.Where(p => p.CategoryId == request.CategoryId);
        if (request.Active is not null)
            query = query.Where(p => p.IsActive == request.Active);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * StoreSupport.AdminPageSize)
            .Take(StoreSupport.AdminPageSize)
            .ToListAsync(cancellationToken);

        var rows = items.Select(p => new AdminProductDto(
            p.Id, p.Name, p.Sku, p.Category.Name, p.Price, p.CalculateDiscountPrice(),
            p.StockQuantity, p.IsActive, p.CreatedAt, p.UpdatedAt)).ToList();

        return Result<PageDto<AdminProductDto>>.Success(new PageDto<AdminProductDto>(rows, page, StoreSupport.AdminPageSize, total));
    }
}

public sealed class GetAdminProductHandler(GholamContext db)
    : IQueryHandler<GetAdminProductQuery, Result<AdminProductEditDto>>
{
    public async Task<Result<AdminProductEditDto>> Handle(GetAdminProductQuery request, CancellationToken cancellationToken)
    {
        var categories = await db.Categories.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Slug, 0))
            .ToListAsync(cancellationToken);

        if (request.Id is null or 0)
        {
            return Result<AdminProductEditDto>.Success(new AdminProductEditDto(
                null, "", "", "", 0, null, 0, "", "", categories.FirstOrDefault()?.Id ?? 0, true, categories));
        }

        var product = await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
        if (product is null)
            return Result<AdminProductEditDto>.NotFound("کالا پیدا نشد.");

        return Result<AdminProductEditDto>.Success(new AdminProductEditDto(
            product.Id, product.Name, product.Slug, product.Description, product.Price, product.DiscountPercent,
            product.StockQuantity, product.Sku, product.ImageUrl, product.CategoryId, product.IsActive, categories));
    }
}

public sealed class SaveProductHandler(GholamContext db) : ICommandHandler<SaveProductCommand, Result<int>>
{
    public async Task<Result<int>> Handle(SaveProductCommand request, CancellationToken cancellationToken)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken);
        if (category is null)
            return StoreSupport.Fail<int>("دسته‌بندی را انتخاب کنید.");

        try
        {
            var slug = await UniqueSlug(db, string.IsNullOrWhiteSpace(request.Slug) ? request.Name : request.Slug, request.Id, cancellationToken);
            var sku = string.IsNullOrWhiteSpace(request.Sku)
                ? $"GM-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}"
                : request.Sku.Trim();

            Product product;
            if (request.Id is null or 0)
            {
                product = new Product(category, request.Name, slug, request.Price, request.Stock, sku, request.Description, request.ImageUrl);
                if (!request.IsActive)
                    product.Update(product.Name, product.Slug, product.Price, product.Sku, product.Description, product.ImageUrl, false);
                db.Products.Add(product);
            }
            else
            {
                product = await db.Products.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
                    ?? throw new DomainException("Product was not found.");
                product.Update(request.Name, slug, request.Price, sku, request.Description, request.ImageUrl, request.IsActive);
                product.UpdateStock(request.Stock);
                product.MoveTo(category);
            }

            product.SetDiscount(request.DiscountPercent);
            await db.SaveChangesAsync(cancellationToken);
            return Result<int>.Success(product.Id, "کالا ذخیره شد.");
        }
        catch (Exception ex) when (ex is DomainException or ArgumentException)
        {
            return StoreSupport.Fail<int>(ex is ArgumentException ? "اطلاعات کالا کامل یا معتبر نیست." : StoreSupport.Persian(ex));
        }
    }

    private static async Task<string> UniqueSlug(GholamContext db, string source, int? id, CancellationToken cancellationToken)
    {
        var slug = StoreSupport.Slugify(source);
        if (string.IsNullOrWhiteSpace(slug))
            slug = $"product-{Guid.NewGuid().ToString("N")[..6]}";

        var candidate = slug;
        var suffix = 2;
        while (await db.Products.AnyAsync(p => p.Slug == candidate && p.Id != id, cancellationToken))
            candidate = $"{slug}-{suffix++}";

        return candidate;
    }
}

public sealed class DeleteProductHandler(GholamContext db) : ICommandHandler<DeleteProductCommand, Result<string>>
{
    public async Task<Result<string>> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
        if (product is null)
            return StoreSupport.Fail<string>("کالا پیدا نشد.");

        var used = await db.OrderItems.AnyAsync(i => i.ProductId == product.Id, cancellationToken);
        if (used)
        {
            product.Update(product.Name, product.Slug, product.Price, product.Sku, product.Description, product.ImageUrl, false);
            await db.SaveChangesAsync(cancellationToken);
            return StoreSupport.Ok("این کالا در سفارش‌ها استفاده شده و غیرفعال شد.");
        }

        db.Products.Remove(product);
        await db.SaveChangesAsync(cancellationToken);
        return StoreSupport.Ok("کالا حذف شد.");
    }
}

public sealed class AdminCategoriesHandler(GholamContext db)
    : IQueryHandler<AdminCategoriesQuery, Result<IReadOnlyList<AdminCategoryDto>>>
{
    public async Task<Result<IReadOnlyList<AdminCategoryDto>>> Handle(AdminCategoriesQuery request, CancellationToken cancellationToken)
    {
        var categories = await db.Categories.AsNoTracking().Include(c => c.Parent).Include(c => c.Products).OrderBy(c => c.DisplayOrder).ToListAsync(cancellationToken);
        var rows = categories.Select(c => new AdminCategoryDto(
            c.Id, c.Name, c.Slug, c.Description, c.DisplayOrder, c.IsActive, c.ParentId, c.Parent?.Name, c.Products.Count)).ToList();
        return Result<IReadOnlyList<AdminCategoryDto>>.Success(rows);
    }
}

public sealed class SaveCategoryHandler(GholamContext db) : ICommandHandler<SaveCategoryCommand, Result<string>>
{
    public async Task<Result<string>> Handle(SaveCategoryCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var slug = StoreSupport.Slugify(string.IsNullOrWhiteSpace(request.Slug) ? request.Name : request.Slug!);
            if (string.IsNullOrWhiteSpace(slug))
                return StoreSupport.Fail<string>("نام دسته معتبر نیست.");

            if (await db.Categories.AnyAsync(c => c.Slug == slug && c.Id != request.Id, cancellationToken))
                slug = $"{slug}-{Guid.NewGuid().ToString("N")[..4]}";

            Category category;
            if (request.Id is null or 0)
            {
                category = new Category(request.Name, slug, request.Description, request.DisplayOrder);
                if (!request.IsActive)
                    category.Update(category.Name, category.Slug, category.Description, request.DisplayOrder, false);
                db.Categories.Add(category);
            }
            else
            {
                category = await db.Categories.Include(c => c.Parent).FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
                    ?? throw new DomainException("Category was not found.");
                category.Update(request.Name, slug, request.Description, request.DisplayOrder, request.IsActive);
            }

            if (request.ParentId is > 0)
            {
                var parent = await db.Categories.FirstOrDefaultAsync(c => c.Id == request.ParentId, cancellationToken)
                    ?? throw new DomainException("Parent category was not found.");
                category.MoveUnder(parent);
            }
            else if (request.Id is > 0)
            {
                category.MoveUnder(null);
            }

            await db.SaveChangesAsync(cancellationToken);
            return StoreSupport.Ok("دسته‌بندی ذخیره شد.");
        }
        catch (Exception ex) when (ex is DomainException or ArgumentException)
        {
            return StoreSupport.Fail<string>(ex is ArgumentException ? "اطلاعات دسته‌بندی کامل نیست." : StoreSupport.Persian(ex));
        }
    }
}

public sealed class DeleteCategoryHandler(GholamContext db) : ICommandHandler<DeleteCategoryCommand, Result<string>>
{
    public async Task<Result<string>> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await db.Categories.Include(c => c.Products).Include(c => c.Children)
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
        if (category is null)
            return StoreSupport.Fail<string>("دسته‌بندی پیدا نشد.");
        if (category.Products.Count > 0 || category.Children.Count > 0)
            return StoreSupport.Fail<string>("دسته‌ای که کالا یا زیردسته دارد را نمی‌توان حذف کرد.");

        db.Categories.Remove(category);
        await db.SaveChangesAsync(cancellationToken);
        return StoreSupport.Ok("دسته‌بندی حذف شد.");
    }
}

public sealed class AdminOrdersHandler(GholamContext db)
    : IQueryHandler<AdminOrdersQuery, Result<PageDto<OrderSummaryDto>>>
{
    public async Task<Result<PageDto<OrderSummaryDto>>> Handle(AdminOrdersQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var query = db.Orders.AsNoTracking().Include(o => o.Payments).Include(o => o.Customer).AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.OrderNumber))
            query = query.Where(o => o.OrderNumber.Contains(request.OrderNumber.Trim()));
        if (!string.IsNullOrWhiteSpace(request.Customer))
            query = query.Where(o => o.Customer.FullName.Contains(request.Customer.Trim()));
        if (request.Status is not null)
            query = query.Where(o => o.Status == request.Status);
        if (request.From is not null)
            query = query.Where(o => o.CreatedAt >= request.From);
        if (request.To is not null)
            query = query.Where(o => o.CreatedAt < request.To.Value.Date.AddDays(1));

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * StoreSupport.AdminPageSize)
            .Take(StoreSupport.AdminPageSize)
            .ToListAsync(cancellationToken);

        return Result<PageDto<OrderSummaryDto>>.Success(
            new PageDto<OrderSummaryDto>(items.Select(StoreSupport.ToSummary).ToList(), page, StoreSupport.AdminPageSize, total));
    }
}

public sealed class UpdateOrderStatusHandler(GholamContext db)
    : ICommandHandler<UpdateOrderStatusCommand, Result<string>>
{
    public async Task<Result<string>> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
    {
        var order = await db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);
        if (order is null)
            return StoreSupport.Fail<string>("سفارش پیدا نشد.");

        try
        {
            if (request.Status == OrderStatus.Cancelled)
                order.Cancel();
            else
                order.UpdateStatus(request.Status);

            await db.SaveChangesAsync(cancellationToken);
            return StoreSupport.Ok("وضعیت سفارش به‌روزرسانی شد.");
        }
        catch (Exception ex) when (ex is DomainException or ArgumentException)
        {
            return StoreSupport.Fail<string>(StoreSupport.Persian(ex));
        }
    }
}

public sealed class AdminUsersHandler(UserManager<User> users, GholamContext db)
    : IQueryHandler<AdminUsersQuery, Result<PageDto<UserRowDto>>>
{
    public async Task<Result<PageDto<UserRowDto>>> Handle(AdminUsersQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var query = users.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(u =>
                (u.Email != null && u.Email.Contains(term)) ||
                (u.FirstName != null && u.FirstName.Contains(term)) ||
                (u.LastName != null && u.LastName.Contains(term)) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(term)));
        }

        var total = await query.CountAsync(cancellationToken);
        var slice = await query.OrderBy(u => u.Email)
            .Skip((page - 1) * StoreSupport.AdminPageSize)
            .Take(StoreSupport.AdminPageSize)
            .ToListAsync(cancellationToken);

        var ids = slice.Select(u => u.Id).ToList();
        var customers = await db.Customers.AsNoTracking().Include(c => c.Orders)
            .Where(c => ids.Contains(c.UserId)).ToListAsync(cancellationToken);

        var rows = new List<UserRowDto>();
        foreach (var user in slice)
        {
            var customer = customers.FirstOrDefault(c => c.UserId == user.Id);
            var isAdmin = await users.IsInRoleAsync(user, DefaultRoles.Admin);
            rows.Add(new UserRowDto(
                user.Id,
                $"{user.FirstName} {user.LastName}".Trim(),
                user.Email,
                user.PhoneNumber,
                isAdmin,
                user.Status == 1 && customer?.IsActive != false,
                customer?.Orders.Count ?? 0));
        }

        return Result<PageDto<UserRowDto>>.Success(new PageDto<UserRowDto>(rows, page, StoreSupport.AdminPageSize, total));
    }
}

public sealed class GetAdminUserHandler(UserManager<User> users, GholamContext db)
    : IQueryHandler<GetAdminUserQuery, Result<AdminUserDto>>
{
    public async Task<Result<AdminUserDto>> Handle(GetAdminUserQuery request, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(request.Id);
        if (user is null)
            return Result<AdminUserDto>.NotFound("کاربر پیدا نشد.");

        var customer = await db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.UserId == user.Id, cancellationToken);
        var orders = customer is null
            ? []
            : await db.Orders.AsNoTracking().Include(o => o.Payments).Include(o => o.Customer)
                .Where(o => o.CustomerId == customer.Id).OrderByDescending(o => o.CreatedAt).ToListAsync(cancellationToken);

        return Result<AdminUserDto>.Success(new AdminUserDto(
            user.Id,
            $"{user.FirstName} {user.LastName}".Trim(),
            user.Email,
            user.PhoneNumber,
            await users.IsInRoleAsync(user, DefaultRoles.Admin),
            user.Status == 1 && customer?.IsActive != false,
            orders.Select(StoreSupport.ToSummary).ToList()));
    }
}

public sealed class SetUserActiveHandler(UserManager<User> users, GholamContext db)
    : ICommandHandler<SetUserActiveCommand, Result<string>>
{
    public async Task<Result<string>> Handle(SetUserActiveCommand request, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(request.Id);
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.UserId == request.Id, cancellationToken);
        if (user is null)
            return StoreSupport.Fail<string>("کاربر پیدا نشد.");

        user.Status = request.Active ? (byte)1 : (byte)0;
        if (customer is not null)
        {
            if (request.Active)
                customer.Activate();
            else
                customer.Deactivate();
        }

        await users.SetLockoutEnabledAsync(user, true);
        await users.SetLockoutEndDateAsync(user, request.Active ? null : DateTimeOffset.MaxValue);
        await db.SaveChangesAsync(cancellationToken);
        return StoreSupport.Ok(request.Active ? "حساب فعال شد." : "حساب غیرفعال شد.");
    }
}

public sealed class SetUserRoleHandler(UserManager<User> users)
    : ICommandHandler<SetUserRoleCommand, Result<string>>
{
    public async Task<Result<string>> Handle(SetUserRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(request.Id);
        if (user is null)
            return StoreSupport.Fail<string>("کاربر پیدا نشد.");

        if (!await users.IsInRoleAsync(user, DefaultRoles.User))
            await users.AddToRoleAsync(user, DefaultRoles.User);

        var isAdmin = await users.IsInRoleAsync(user, DefaultRoles.Admin);
        if (request.Admin && !isAdmin)
            await users.AddToRoleAsync(user, DefaultRoles.Admin);
        if (!request.Admin && isAdmin)
            await users.RemoveFromRoleAsync(user, DefaultRoles.Admin);

        return StoreSupport.Ok("نقش کاربر ذخیره شد.");
    }
}
