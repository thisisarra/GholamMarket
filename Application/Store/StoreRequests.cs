using Domain.Entities.Sales;
using NibFramework.Commands;
using NibFramework.Model;
using NibFramework.Queries;

namespace Application.Store;

public sealed record GetHomeQuery : IQuery<Result<HomeDto>>;

public sealed record GetProductsQuery(string? Search, string? CategorySlug, int Page)
    : IQuery<Result<ProductListDto>>;

public sealed record GetProductQuery(string Slug) : IQuery<Result<ProductDetailDto>>;

public sealed record GetCategoriesQuery : IQuery<Result<IReadOnlyList<CategoryDto>>>;

public sealed record GetCartQuery : IQuery<Result<CartDto>>;

public sealed record AddToCartCommand(int ProductId, int Quantity) : ICommand<Result<string>>;

public sealed record UpdateCartItemCommand(int ProductId, int Quantity) : ICommand<Result<string>>;

public sealed record RemoveCartItemCommand(int ProductId) : ICommand<Result<string>>;

public sealed record GetCheckoutQuery : IQuery<Result<CheckoutDto>>;

public sealed record PlaceOrderCommand(int AddressId, string? CouponCode, string? Note)
    : ICommand<Result<int>>;

public sealed record GetPaymentQuery(int OrderId) : IQuery<Result<PaymentDto>>;

public sealed record VerifyPaymentCommand(int PaymentId, bool Approved) : ICommand<Result<int>>;

public sealed record RegisterCommand(
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    string Password) : ICommand<Result<string>>;

public sealed record LoginCommand(string Identifier, string Password) : ICommand<Result<string>>;

public sealed record LogoutCommand : ICommand<Result<string>>;

public sealed record GetDashboardQuery : IQuery<Result<DashboardDto>>;

public sealed record GetProfileQuery : IQuery<Result<ProfileDto>>;

public sealed record UpdateProfileCommand(string FirstName, string LastName, string Email, string Phone)
    : ICommand<Result<string>>;

public sealed record UpdatePasswordCommand(string CurrentPassword, string NewPassword)
    : ICommand<Result<string>>;

public sealed record GetAddressesQuery : IQuery<Result<IReadOnlyList<AddressDto>>>;

public sealed record SaveAddressCommand(
    int? Id,
    string Title,
    string ReceiverName,
    string ReceiverPhone,
    string Province,
    string City,
    string Street,
    string PostalCode,
    string? Plaque,
    string? Unit,
    bool IsDefault) : ICommand<Result<string>>;

public sealed record DeleteAddressCommand(int Id) : ICommand<Result<string>>;

public sealed record SetDefaultAddressCommand(int Id) : ICommand<Result<string>>;

public sealed record GetMyOrdersQuery : IQuery<Result<IReadOnlyList<OrderSummaryDto>>>;

public sealed record GetOrderQuery(int Id) : IQuery<Result<OrderDetailDto>>;

public sealed record CancelOrderCommand(int Id) : ICommand<Result<string>>;

public sealed record SubmitContactCommand(string Name, string Email, string Message)
    : ICommand<Result<string>>;

public sealed record AdminDashboardQuery : IQuery<Result<AdminDashboardDto>>;

public sealed record AdminProductsQuery(string? Search, int? CategoryId, bool? Active, int Page)
    : IQuery<Result<PageDto<AdminProductDto>>>;

public sealed record GetAdminProductQuery(int? Id) : IQuery<Result<AdminProductEditDto>>;

public sealed record SaveProductCommand(
    int? Id,
    string Name,
    string? Slug,
    string? Description,
    decimal Price,
    decimal? DiscountPercent,
    int Stock,
    string? Sku,
    string? ImageUrl,
    int CategoryId,
    bool IsActive) : ICommand<Result<int>>;

public sealed record DeleteProductCommand(int Id) : ICommand<Result<string>>;

public sealed record AdminCategoriesQuery : IQuery<Result<IReadOnlyList<AdminCategoryDto>>>;

public sealed record SaveCategoryCommand(
    int? Id,
    string Name,
    string? Slug,
    string? Description,
    int DisplayOrder,
    bool IsActive,
    int? ParentId) : ICommand<Result<string>>;

public sealed record DeleteCategoryCommand(int Id) : ICommand<Result<string>>;

public sealed record AdminOrdersQuery(
    string? OrderNumber,
    string? Customer,
    OrderStatus? Status,
    DateTime? From,
    DateTime? To,
    int Page) : IQuery<Result<PageDto<OrderSummaryDto>>>;

public sealed record UpdateOrderStatusCommand(int Id, OrderStatus Status) : ICommand<Result<string>>;

public sealed record AdminUsersQuery(string? Search, int Page) : IQuery<Result<PageDto<UserRowDto>>>;

public sealed record GetAdminUserQuery(string Id) : IQuery<Result<AdminUserDto>>;

public sealed record SetUserActiveCommand(string Id, bool Active) : ICommand<Result<string>>;

public sealed record SetUserRoleCommand(string Id, bool Admin) : ICommand<Result<string>>;
