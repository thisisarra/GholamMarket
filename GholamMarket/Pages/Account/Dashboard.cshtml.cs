namespace GholamMarket.Pages.Account;

public class DashboardModel(IMediator mediator) : PageModel
{
    public DashboardDto? Dashboard { get; private set; }

    public async Task OnGetAsync()
    {
        var result = await mediator.Send(new GetDashboardQuery());
        if (!result.IsSuccess || result.ResultBody is null)
            TempData["Error"] = result.Message;
        else
            Dashboard = result.ResultBody;
    }
}
