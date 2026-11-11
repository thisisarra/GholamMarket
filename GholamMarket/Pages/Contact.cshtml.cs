namespace GholamMarket.Pages;

public class ContactModel(IMediator mediator) : PageModel
{
    [BindProperty, Required(ErrorMessage = "نام را وارد کنید.")]
    public string Name { get; set; } = "";

    [BindProperty, Required(ErrorMessage = "ایمیل را وارد کنید."), EmailAddress(ErrorMessage = "ایمیل معتبر نیست.")]
    public string Email { get; set; } = "";

    [BindProperty, Required(ErrorMessage = "پیام را بنویسید."), MinLength(10, ErrorMessage = "پیام خیلی کوتاه است.")]
    public string Message { get; set; } = "";

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var result = await mediator.Send(new SubmitContactCommand(Name, Email, Message));
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "پیام ثبت نشد.");
            return Page();
        }

        TempData["Success"] = result.Message;
        return RedirectToPage();
    }
}
