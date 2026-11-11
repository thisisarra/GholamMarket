using Domain.Entities.Sales;

namespace Application.Store;

public sealed record PageDto<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total)
{
    public int PageCount => PageSize == 0 ? 0 : (int)Math.Ceiling(Total / (double)PageSize);
}

public sealed record CategoryDto(int Id, string Name, string Slug, int ProductCount);

public sealed record ProductCardDto(
    int Id,
    string Name,
    string Slug,
    string? ImageUrl,
    decimal Price,
    decimal FinalPrice,
    decimal? DiscountPercent,
    int Stock,
    string CategoryName);

public sealed record ProductDetailDto(
    int Id,
    string Name,
    string Slug,
    string? Description,
    string? ImageUrl,
    decimal Price,
    decimal FinalPrice,
    decimal? DiscountPercent,
    int Stock,
    bool IsActive,
    string CategoryName,
    string CategorySlug,
    IReadOnlyList<AttributeDto> Attributes);

public sealed record AttributeDto(string Name, string Value);

public sealed record ProductListDto(
    PageDto<ProductCardDto> Page,
    IReadOnlyList<CategoryDto> Categories,
    string? Search,
    string? CategorySlug);

public sealed record HomeDto(
    IReadOnlyList<CategoryDto> Categories,
    IReadOnlyList<ProductCardDto> Products);

public sealed record CartLineDto(
    int ProductId,
    string Name,
    string Slug,
    string? ImageUrl,
    decimal UnitPrice,
    decimal ListPrice,
    int Quantity,
    int Stock,
    bool IsActive,
    decimal LineTotal);

public sealed record CartDto(
    IReadOnlyList<CartLineDto> Lines,
    decimal Subtotal,
    int ItemCount,
    bool PriceChanged)
{
    public static CartDto Empty { get; } = new([], 0, 0, false);
}

public sealed record AddressDto(
    int Id,
    string Title,
    string ReceiverName,
    string ReceiverPhone,
    string Province,
    string City,
    string Street,
    string? Plaque,
    string? Unit,
    string PostalCode,
    bool IsDefault);

public sealed record CheckoutDto(
    CartDto Cart,
    IReadOnlyList<AddressDto> Addresses,
    decimal Shipping,
    decimal Total);

public sealed record PaymentDto(
    int PaymentId,
    int OrderId,
    string OrderNumber,
    decimal Amount,
    PaymentStatus Status);

public sealed record OrderSummaryDto(
    int Id,
    string OrderNumber,
    DateTime CreatedAt,
    decimal TotalAmount,
    OrderStatus Status,
    PaymentStatus? PaymentStatus,
    string CustomerName);

public sealed record OrderLineDto(
    string ProductName,
    string Sku,
    int Quantity,
    decimal Price,
    decimal LineTotal);

public sealed record OrderDetailDto(
    int Id,
    string OrderNumber,
    DateTime CreatedAt,
    OrderStatus Status,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal ShippingAmount,
    decimal TotalAmount,
    string? Note,
    string ReceiverName,
    string ReceiverPhone,
    string Province,
    string City,
    string Street,
    string? Plaque,
    string PostalCode,
    string? TrackingCode,
    PaymentStatus? PaymentStatus,
    string CustomerName,
    string? CustomerEmail,
    IReadOnlyList<OrderLineDto> Lines);

public sealed record DashboardDto(
    string FullName,
    string? Email,
    int TotalOrders,
    int PendingPayment,
    int Processing,
    IReadOnlyList<OrderSummaryDto> RecentOrders);

public sealed record ProfileDto(
    string FirstName,
    string LastName,
    string? Email,
    string? Phone);

public sealed record AdminDashboardDto(
    int Users,
    int Products,
    int ActiveProducts,
    int Orders,
    int PendingPayment,
    int Paid,
    int Processing,
    int Completed,
    decimal Sales,
    IReadOnlyList<OrderSummaryDto> LatestOrders);

public sealed record AdminProductDto(
    int Id,
    string Name,
    string Sku,
    string CategoryName,
    decimal Price,
    decimal FinalPrice,
    int Stock,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record AdminProductEditDto(
    int? Id,
    string Name,
    string Slug,
    string? Description,
    decimal Price,
    decimal? DiscountPercent,
    int Stock,
    string Sku,
    string? ImageUrl,
    int CategoryId,
    bool IsActive,
    IReadOnlyList<CategoryDto> Categories);

public sealed record AdminCategoryDto(
    int Id,
    string Name,
    string Slug,
    string? Description,
    int DisplayOrder,
    bool IsActive,
    int? ParentId,
    string? ParentName,
    int ProductCount);

public sealed record UserRowDto(
    string Id,
    string Name,
    string? Email,
    string? Phone,
    bool IsAdmin,
    bool IsActive,
    int OrderCount);

public sealed record AdminUserDto(
    string Id,
    string Name,
    string? Email,
    string? Phone,
    bool IsAdmin,
    bool IsActive,
    IReadOnlyList<OrderSummaryDto> Orders);
