namespace GholamMarket.Areas.Admin.Pages.Categories;

public class IndexModel(IMediator mediator) : PageModel
{
    public IReadOnlyList<AdminCategoryDto> Categories { get; private set; } = [];

    [BindProperty]
    public InputModel Input { get; set; } = new() { IsActive = true };

    public class InputModel
    {
        public int? Id { get; set; }
        [Required(ErrorMessage = "نام دسته الزامی است.")] public string Name { get; set; } = "";
        public string? Slug { get; set; }
        public string? Description { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public int? ParentId { get; set; }
    }

    public async Task OnGetAsync(int? id)
    {
        await LoadAsync();
        var selected = Categories.FirstOrDefault(c => c.Id == id);
        if (selected is null)
            return;

        Input = new InputModel
        {
            Id = selected.Id,
            Name = selected.Name,
            Slug = selected.Slug,
            Description = selected.Description,
            DisplayOrder = selected.DisplayOrder,
            IsActive = selected.IsActive,
            ParentId = selected.ParentId
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        var result = await mediator.Send(new SaveCategoryCommand(
            Input.Id, Input.Name, Input.Slug, Input.Description, Input.DisplayOrder, Input.IsActive,
            Input.ParentId is > 0 ? Input.ParentId : null));
        TempData[result.IsSuccess ? "Success" : "Error"] = result.Message;
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var result = await mediator.Send(new DeleteCategoryCommand(id));
        TempData[result.IsSuccess ? "Success" : "Error"] = result.Message;
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        var result = await mediator.Send(new AdminCategoriesQuery());
        Categories = result.ResultBody ?? [];
        if (!result.IsSuccess)
            TempData["Error"] = result.Message;
    }
}
