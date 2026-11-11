namespace GholamMarket.Pages.Account;

public class LogoutModel(IMediator mediator) : PageModel
{
    public async Task<IActionResult> OnPostAsync()
    {
        await mediator.Send(new LogoutCommand());
        return RedirectToPage("/Index");
    }

    public IActionResult OnGet() => RedirectToPage("/Index");
}
