using Domain.Entities.Sales;

namespace GholamMarket.Areas.Admin.Pages.Orders;

public class DetailsModel(IMediator mediator) : PageModel
{
    public OrderDetailDto? Order { get; private set; }

    [BindProperty]
    public OrderStatus Status { get; set; }

    public async Task<IActionResult> OnGetAsync(int id) => await LoadAsync(id);

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var result = await mediator.Send(new UpdateOrderStatusCommand(id, Status));
        TempData[result.IsSuccess ? "Success" : "Error"] = result.Message;
        return RedirectToPage(new { id });
    }

    private async Task<IActionResult> LoadAsync(int id)
    {
        var result = await mediator.Send(new GetOrderQuery(id));
        if (!result.IsSuccess || result.ResultBody is null)
        {
            TempData["Error"] = result.Message ?? "سفارش پیدا نشد.";
            return RedirectToPage("Index");
        }

        Order = result.ResultBody;
        Status = Order.Status;
        return Page();
    }
}
