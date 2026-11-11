namespace GholamMarket.Pages.Checkout;

public class ResultModel(IMediator mediator) : PageModel
{
    public OrderDetailDto? Order { get; private set; }

    public async Task<IActionResult> OnGetAsync(int orderId)
    {
        var result = await mediator.Send(new GetOrderQuery(orderId));
        if (!result.IsSuccess || result.ResultBody is null)
        {
            TempData["Error"] = result.Message;
            return RedirectToPage("/Account/Orders/Index");
        }

        Order = result.ResultBody;
        return Page();
    }
}
