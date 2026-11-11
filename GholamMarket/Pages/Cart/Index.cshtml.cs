namespace GholamMarket.Pages.Cart;

public class IndexModel(IMediator mediator) : PageModel
{
    public CartDto Cart { get; private set; } = CartDto.Empty;

    public async Task OnGetAsync()
    {
        var result = await mediator.Send(new GetCartQuery());
        Cart = result.ResultBody ?? CartDto.Empty;
        if (Cart.PriceChanged)
            TempData["Error"] = "قیمت بعضی کالاها تغییر کرده است. مبلغ‌ها به‌روز شد.";
    }

    public async Task<IActionResult> OnPostUpdateAsync(int productId, int quantity)
    {
        var result = await mediator.Send(new UpdateCartItemCommand(productId, quantity));
        TempData[result.IsSuccess ? "Success" : "Error"] = result.Message;
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRemoveAsync(int productId)
    {
        var result = await mediator.Send(new RemoveCartItemCommand(productId));
        TempData[result.IsSuccess ? "Success" : "Error"] = result.Message;
        return RedirectToPage();
    }
}
