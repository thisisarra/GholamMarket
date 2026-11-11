namespace GholamMarket.Areas.Admin.Pages;

public class IndexModel(IMediator mediator) : PageModel
{
    public AdminDashboardDto? Dashboard { get; private set; }

    public async Task OnGetAsync()
    {
        var result = await mediator.Send(new AdminDashboardQuery());
        if (!result.IsSuccess || result.ResultBody is null)
            TempData["Error"] = result.Message;
        else
            Dashboard = result.ResultBody;
    }
}
