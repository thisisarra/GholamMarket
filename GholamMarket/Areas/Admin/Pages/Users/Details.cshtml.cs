namespace GholamMarket.Areas.Admin.Pages.Users;

public class DetailsModel(IMediator mediator) : PageModel
{
    public AdminUserDto? Account { get; private set; }

    public async Task<IActionResult> OnGetAsync(string id) => await LoadAsync(id);

    public async Task<IActionResult> OnPostActiveAsync(string id, bool active)
    {
        var result = await mediator.Send(new SetUserActiveCommand(id, active));
        TempData[result.IsSuccess ? "Success" : "Error"] = result.Message;
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRoleAsync(string id, bool admin)
    {
        var result = await mediator.Send(new SetUserRoleCommand(id, admin));
        TempData[result.IsSuccess ? "Success" : "Error"] = result.Message;
        return RedirectToPage(new { id });
    }

    private async Task<IActionResult> LoadAsync(string id)
    {
        var result = await mediator.Send(new GetAdminUserQuery(id));
        if (!result.IsSuccess || result.ResultBody is null)
        {
            TempData["Error"] = result.Message;
            return RedirectToPage("Index");
        }

        Account = result.ResultBody;
        return Page();
    }
}
