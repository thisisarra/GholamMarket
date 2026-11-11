namespace GholamMarket.Pages.Checkout;

public class PayModel(IMediator mediator) : PageModel
{
    public PaymentDto? Payment { get; private set; }

    public async Task<IActionResult> OnGetAsync(int orderId)
    {
        var result = await mediator.Send(new GetPaymentQuery(orderId));
        if (!result.IsSuccess || result.ResultBody is null)
        {
            TempData["Error"] = result.Message;
            return RedirectToPage("/Account/Orders/Index");
        }

        Payment = result.ResultBody;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int paymentId, bool approved)
    {
        var result = await mediator.Send(new VerifyPaymentCommand(paymentId, approved));
        TempData[result.IsSuccess ? "Success" : "Error"] = result.Message;
        if (result.ResultBody > 0)
            return RedirectToPage("/Checkout/Result", new { orderId = result.ResultBody });

        return RedirectToPage("/Account/Orders/Index");
    }
}
