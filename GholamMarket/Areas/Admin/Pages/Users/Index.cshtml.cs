namespace GholamMarket.Areas.Admin.Pages.Users;

public class IndexModel(IMediator mediator) : PageModel
{
    public PageDto<UserRowDto> PageData { get; private set; } = new([], 1, 15, 0);
    public string? Search { get; private set; }

    public async Task OnGetAsync(string? search, int page = 1)
    {
        Search = search;
        var result = await mediator.Send(new AdminUsersQuery(search, page));
        if (result.IsSuccess && result.ResultBody is not null)
            PageData = result.ResultBody;
        else
            TempData["Error"] = result.Message;
    }
}
