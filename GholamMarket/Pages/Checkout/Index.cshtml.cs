namespace GholamMarket.Pages.Checkout;

public class IndexModel(IMediator mediator) : PageModel
{
    public CheckoutDto? Checkout { get; private set; }

    [BindProperty]
    public int AddressId { get; set; }

    [BindProperty]
    public string? CouponCode { get; set; }

    [BindProperty]
    public string? Note { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        return await LoadAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (AddressId <= 0)
        {
            TempData["Error"] = "یک آدرس تحویل انتخاب کنید.";
            return await LoadAsync();
        }

        var result = await mediator.Send(new PlaceOrderCommand(AddressId, CouponCode, Note));
        if (!result.IsSuccess || result.ResultBody == 0)
        {
            TempData["Error"] = result.Message;
            return await LoadAsync();
        }

        TempData["Success"] = result.Message;
        return RedirectToPage("/Checkout/Pay", new { orderId = result.ResultBody });
    }

    private async Task<IActionResult> LoadAsync()
    {
        var result = await mediator.Send(new GetCheckoutQuery());
        if (!result.IsSuccess || result.ResultBody is null)
        {
            TempData["Error"] = result.Message;
            return RedirectToPage("/Cart/Index");
        }

        Checkout = result.ResultBody;
        if (Checkout.Cart.Lines.Count == 0)
        {
            TempData["Error"] = "سبد خرید خالی است.";
            return RedirectToPage("/Cart/Index");
        }

        if (Checkout.Cart.PriceChanged)
            TempData["Error"] = "قیمت برخی کالاها تغییر کرده و مبلغ سفارش به‌روز شده است.";

        if (AddressId == 0)
            AddressId = Checkout.Addresses.FirstOrDefault(a => a.IsDefault)?.Id ?? Checkout.Addresses.FirstOrDefault()?.Id ?? 0;

        return Page();
    }
}
