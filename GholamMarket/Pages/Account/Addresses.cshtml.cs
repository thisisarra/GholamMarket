namespace GholamMarket.Pages.Account;

public class AddressesModel(IMediator mediator) : PageModel
{
    public IReadOnlyList<AddressDto> Addresses { get; private set; } = [];

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        public int? Id { get; set; }
        [Required(ErrorMessage = "عنوان آدرس را وارد کنید.")] public string Title { get; set; } = "";
        [Required(ErrorMessage = "نام گیرنده را وارد کنید.")] public string ReceiverName { get; set; } = "";
        [Required(ErrorMessage = "موبایل گیرنده را وارد کنید.")] public string ReceiverPhone { get; set; } = "";
        [Required(ErrorMessage = "استان را وارد کنید.")] public string Province { get; set; } = "";
        [Required(ErrorMessage = "شهر را وارد کنید.")] public string City { get; set; } = "";
        [Required(ErrorMessage = "نشانی را وارد کنید.")] public string Street { get; set; } = "";
        [Required(ErrorMessage = "کد پستی را وارد کنید.")] public string PostalCode { get; set; } = "";
        public string? Plaque { get; set; }
        public string? Unit { get; set; }
        public bool IsDefault { get; set; }
    }

    public async Task OnGetAsync(int? id)
    {
        await LoadAsync();
        var selected = Addresses.FirstOrDefault(a => a.Id == id);
        if (selected is null)
            return;

        Input = new InputModel
        {
            Id = selected.Id,
            Title = selected.Title,
            ReceiverName = selected.ReceiverName,
            ReceiverPhone = selected.ReceiverPhone,
            Province = selected.Province,
            City = selected.City,
            Street = selected.Street,
            PostalCode = selected.PostalCode,
            Plaque = selected.Plaque,
            Unit = selected.Unit,
            IsDefault = selected.IsDefault
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        var result = await mediator.Send(new SaveAddressCommand(
            Input.Id, Input.Title, Input.ReceiverName, Input.ReceiverPhone, Input.Province,
            Input.City, Input.Street, Input.PostalCode, Input.Plaque, Input.Unit, Input.IsDefault));
        TempData[result.IsSuccess ? "Success" : "Error"] = result.Message;
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var result = await mediator.Send(new DeleteAddressCommand(id));
        TempData[result.IsSuccess ? "Success" : "Error"] = result.Message;
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDefaultAsync(int id)
    {
        var result = await mediator.Send(new SetDefaultAddressCommand(id));
        TempData[result.IsSuccess ? "Success" : "Error"] = result.Message;
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        var result = await mediator.Send(new GetAddressesQuery());
        Addresses = result.ResultBody ?? [];
        if (!result.IsSuccess)
            TempData["Error"] = result.Message;
    }
}
