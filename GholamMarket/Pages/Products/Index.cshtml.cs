namespace GholamMarket.Pages.Products;

public class IndexModel(IMediator mediator) : PageModel
{
    public ProductListDto Listing { get; private set; } = new(new PageDto<ProductCardDto>([], 1, 12, 0), [], null, null);

    public async Task OnGetAsync(string? search, string? category, int page = 1)
    {
        var result = await mediator.Send(new GetProductsQuery(search, category, page));
        if (result.IsSuccess && result.ResultBody is not null)
            Listing = result.ResultBody;
        else
            TempData["Error"] = result.Message;
    }
}
