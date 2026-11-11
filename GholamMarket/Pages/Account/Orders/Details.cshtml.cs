using Domain.Entities.Sales;

namespace GholamMarket.Pages.Account.Orders;

public class DetailsModel(IMediator mediator) : PageModel
{
    public OrderDetailDto? Order { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var result = await mediator.Send(new GetOrderQuery(id));
        if (!result.IsSuccess || result.ResultBody is null)
        {
            TempData["Error"] = result.Message ?? "سفارش پیدا نشد.";
            return RedirectToPage("/Account/Orders/Index");
        }

        Order = result.ResultBody;
        return Page();
    }

    public async Task<IActionResult> OnPostCancelAsync(int id)
    {
        var result = await mediator.Send(new CancelOrderCommand(id));
        TempData[result.IsSuccess ? "Success" : "Error"] = result.Message;
        return RedirectToPage(new { id });
    }
}
