namespace GholamMarket.Pages;

public class IndexModel(IMediator mediator) : PageModel
{
    public HomeDto Home { get; private set; } = new([], []);

    public async Task OnGetAsync()
    {
        var result = await mediator.Send(new GetHomeQuery());
        if (result.IsSuccess && result.ResultBody is not null)
            Home = result.ResultBody;
        else
            TempData["Error"] = result.Message;
    }
}
