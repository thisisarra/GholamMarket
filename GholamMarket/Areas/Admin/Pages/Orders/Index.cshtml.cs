using Domain.Entities.Sales;

namespace GholamMarket.Areas.Admin.Pages.Orders;

public class IndexModel(IMediator mediator) : PageModel
{
    public PageDto<OrderSummaryDto> PageData { get; private set; } = new([], 1, 15, 0);
    public string? OrderNumber { get; private set; }
    public string? CustomerName { get; private set; }
    public OrderStatus? Status { get; private set; }
    public DateTime? From { get; private set; }
    public DateTime? To { get; private set; }

    public async Task OnGetAsync(string? orderNumber, string? customer, OrderStatus? status, DateTime? from, DateTime? to, int page = 1)
    {
        OrderNumber = orderNumber;
        CustomerName = customer;
        Status = status;
        From = from;
        To = to;
        var result = await mediator.Send(new AdminOrdersQuery(orderNumber, customer, status, from, to, page));
        if (result.IsSuccess && result.ResultBody is not null)
            PageData = result.ResultBody;
        else
            TempData["Error"] = result.Message;
    }
}
