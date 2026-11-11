namespace GholamMarket.Pages.Account;

public class RegisterModel(IMediator mediator) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "نام را وارد کنید.")]
        public string FirstName { get; set; } = "";

        [Required(ErrorMessage = "نام خانوادگی را وارد کنید.")]
        public string LastName { get; set; } = "";

        [Required(ErrorMessage = "ایمیل را وارد کنید.")]
        [EmailAddress(ErrorMessage = "ایمیل معتبر نیست.")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "موبایل را وارد کنید.")]
        [Phone(ErrorMessage = "شماره موبایل معتبر نیست.")]
        public string Phone { get; set; } = "";

        [Required(ErrorMessage = "رمز عبور را وارد کنید.")]
        [MinLength(6, ErrorMessage = "رمز عبور باید حداقل ۶ کاراکتر باشد.")]
        public string Password { get; set; } = "";
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var result = await mediator.Send(new RegisterCommand(
            Input.FirstName, Input.LastName, Input.Email, Input.Phone, Input.Password));
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "ثبت‌نام انجام نشد.");
            return Page();
        }

        return RedirectToPage("/Account/Dashboard");
    }
}
