namespace GholamMarket.Pages.Account;

public class ProfileModel(IMediator mediator) : PageModel
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
        public string Phone { get; set; } = "";
    }

    public async Task OnGetAsync()
    {
        var result = await mediator.Send(new GetProfileQuery());
        if (!result.IsSuccess || result.ResultBody is null)
        {
            TempData["Error"] = result.Message;
            return;
        }

        Input = new InputModel
        {
            FirstName = result.ResultBody.FirstName,
            LastName = result.ResultBody.LastName,
            Email = result.ResultBody.Email ?? "",
            Phone = result.ResultBody.Phone ?? ""
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var result = await mediator.Send(new UpdateProfileCommand(Input.FirstName, Input.LastName, Input.Email, Input.Phone));
        TempData[result.IsSuccess ? "Success" : "Error"] = result.Message;
        return RedirectToPage();
    }
}
