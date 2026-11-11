namespace GholamMarket.Areas.Admin.Pages.Products;

public class IndexModel(IMediator mediator) : PageModel
{
    public PageDto<AdminProductDto> PageData { get; private set; } = new([], 1, 15, 0);
    public IReadOnlyList<CategoryDto> Categories { get; private set; } = [];
    public string? Search { get; private set; }
    public int? CategoryId { get; private set; }
    public string? Active { get; private set; }

    public async Task OnGetAsync(string? search, int? categoryId, string? active, int page = 1)
    {
        Search = search;
        CategoryId = categoryId;
        Active = active;
        bool? flag = active switch { "true" => true, "false" => false, _ => null };
        var result = await mediator.Send(new AdminProductsQuery(search, categoryId, flag, page));
        if (result.IsSuccess && result.ResultBody is not null)
            PageData = result.ResultBody;
        else
            TempData["Error"] = result.Message;

        var form = await mediator.Send(new GetAdminProductQuery(null));
        Categories = form.ResultBody?.Categories ?? [];
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var result = await mediator.Send(new DeleteProductCommand(id));
        TempData[result.IsSuccess ? "Success" : "Error"] = result.Message;
        return RedirectToPage();
    }
}
