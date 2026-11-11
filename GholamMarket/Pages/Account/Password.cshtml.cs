namespace GholamMarket.Pages.Account;

public class PasswordModel(IMediator mediator) : PageModel
{
    [BindProperty, Required(ErrorMessage = "رمز فعلی را وارد کنید.")]
    public string CurrentPassword { get; set; } = "";

    [BindProperty, Required(ErrorMessage = "رمز جدید را وارد کنید."), MinLength(6)]
    public string NewPassword { get; set; } = "";

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var result = await mediator.Send(new UpdatePasswordCommand(CurrentPassword, NewPassword));
        TempData[result.IsSuccess ? "Success" : "Error"] = result.Message;
        return RedirectToPage();
    }
}
