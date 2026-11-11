namespace GholamMarket.Pages.Products;

public class DetailsModel(IMediator mediator) : PageModel
{
    public ProductDetailDto? Product { get; private set; }

    [BindProperty]
    [Range(1, 100, ErrorMessage = "تعداد باید بین ۱ تا ۱۰۰ باشد.")]
    public int Quantity { get; set; } = 1;

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        var result = await mediator.Send(new GetProductQuery(slug));
        if (!result.IsSuccess || result.ResultBody is null)
        {
            TempData["Error"] = result.Message ?? "کالا پیدا نشد.";
            return RedirectToPage("/Products/Index");
        }

        Product = result.ResultBody;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string slug)
    {
        var current = await mediator.Send(new GetProductQuery(slug));
        Product = current.ResultBody;
        if (!ModelState.IsValid || Product is null)
            return Page();

        if (User.Identity?.IsAuthenticated != true)
            return RedirectToPage("/Account/Login", new { returnUrl = $"/Products/Details/{slug}" });

        var result = await mediator.Send(new AddToCartCommand(Product.Id, Quantity));
        TempData[result.IsSuccess ? "Success" : "Error"] = result.Message;
        return result.IsSuccess
            ? RedirectToPage("/Cart/Index")
            : RedirectToPage(new { slug });
    }
}
