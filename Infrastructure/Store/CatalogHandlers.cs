using Application.Store;
using Infrastructure.DBContexts;
using Microsoft.EntityFrameworkCore;
using NibFramework.Model;
using NibFramework.Queries;

namespace Infrastructure.Store;

public sealed class GetHomeHandler(GholamContext db) : IQueryHandler<GetHomeQuery, Result<HomeDto>>
{
    public async Task<Result<HomeDto>> Handle(GetHomeQuery request, CancellationToken cancellationToken)
    {
        var categories = await ActiveCategories(db, cancellationToken);
        var products = await db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.Category.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .Take(8)
            .ToListAsync(cancellationToken);

        return Result<HomeDto>.Success(new HomeDto(categories, products.Select(StoreSupport.ToCard).ToList()));
    }

    internal static async Task<IReadOnlyList<CategoryDto>> ActiveCategories(GholamContext db, CancellationToken cancellationToken)
        => await db.Categories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Slug, c.Products.Count(p => p.IsActive)))
            .ToListAsync(cancellationToken);
}

public sealed class GetCategoriesHandler(GholamContext db)
    : IQueryHandler<GetCategoriesQuery, Result<IReadOnlyList<CategoryDto>>>
{
    public async Task<Result<IReadOnlyList<CategoryDto>>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
        => Result<IReadOnlyList<CategoryDto>>.Success(await GetHomeHandler.ActiveCategories(db, cancellationToken));
}

public sealed class GetProductsHandler(GholamContext db)
    : IQueryHandler<GetProductsQuery, Result<ProductListDto>>
{
    public async Task<Result<ProductListDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var query = db.Products.AsNoTracking().Include(p => p.Category).Where(p => p.IsActive && p.Category.IsActive);

        if (!string.IsNullOrWhiteSpace(request.CategorySlug))
            query = query.Where(p => p.Category.Slug == request.CategorySlug);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(p => p.Name.Contains(term) || (p.Description != null && p.Description.Contains(term)));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * StoreSupport.StorePageSize)
            .Take(StoreSupport.StorePageSize)
            .ToListAsync(cancellationToken);

        var categories = await GetHomeHandler.ActiveCategories(db, cancellationToken);
        var pageDto = new PageDto<ProductCardDto>(
            items.Select(StoreSupport.ToCard).ToList(),
            page,
            StoreSupport.StorePageSize,
            total);

        return Result<ProductListDto>.Success(new ProductListDto(pageDto, categories, request.Search, request.CategorySlug));
    }
}

public sealed class GetProductHandler(GholamContext db)
    : IQueryHandler<GetProductQuery, Result<ProductDetailDto>>
{
    public async Task<Result<ProductDetailDto>> Handle(GetProductQuery request, CancellationToken cancellationToken)
    {
        var product = await db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Attributes)
            .FirstOrDefaultAsync(p => p.Slug == request.Slug && p.IsActive, cancellationToken);

        if (product is null)
            return Result<ProductDetailDto>.NotFound("کالا پیدا نشد.");

        var dto = new ProductDetailDto(
            product.Id,
            product.Name,
            product.Slug,
            product.Description,
            product.ImageUrl,
            product.Price,
            product.CalculateDiscountPrice(),
            product.DiscountPercent,
            product.StockQuantity,
            product.IsActive,
            product.Category.Name,
            product.Category.Slug,
            product.Attributes
                .OrderBy(a => a.DisplayOrder)
                .Select(a => new AttributeDto(a.Name, a.Value))
                .ToList());

        return Result<ProductDetailDto>.Success(dto);
    }
}
