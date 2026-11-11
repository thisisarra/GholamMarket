namespace GholamMarket.Pages.Account.Orders;

public class IndexModel(IMediator mediator) : PageModel
{
    public IReadOnlyList<OrderSummaryDto> Orders { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var result = await mediator.Send(new GetMyOrdersQuery());
        Orders = result.ResultBody ?? [];
        if (!result.IsSuccess)
            TempData["Error"] = result.Message;
    }
}
