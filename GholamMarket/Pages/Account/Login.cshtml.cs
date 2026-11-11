namespace GholamMarket.Pages.Account;

public class LoginModel(IMediator mediator) : PageModel
{
    [BindProperty]
    [Required(ErrorMessage = "ایمیل یا موبایل را وارد کنید.")]
    public string Identifier { get; set; } = "";

    [BindProperty]
    [Required(ErrorMessage = "رمز عبور را وارد کنید.")]
    public string Password { get; set; } = "";

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var result = await mediator.Send(new LoginCommand(Identifier, Password));
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "ورود انجام نشد.");
            return Page();
        }

        if (!string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            return Redirect(ReturnUrl);

        return RedirectToPage("/Account/Dashboard");
    }
}
