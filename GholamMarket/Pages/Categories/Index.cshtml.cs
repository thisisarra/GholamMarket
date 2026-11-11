namespace GholamMarket.Pages;

public class CategoriesModel(IMediator mediator) : PageModel
{
    public IReadOnlyList<CategoryDto> Categories { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var result = await mediator.Send(new GetCategoriesQuery());
        Categories = result.ResultBody ?? [];
        if (!result.IsSuccess)
            TempData["Error"] = result.Message;
    }
}
