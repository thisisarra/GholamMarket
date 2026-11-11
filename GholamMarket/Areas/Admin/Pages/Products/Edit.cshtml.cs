namespace GholamMarket.Areas.Admin.Pages.Products;

public class EditModel(IMediator mediator, IWebHostEnvironment environment) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public IReadOnlyList<CategoryDto> Categories { get; private set; } = [];

    public class InputModel
    {
        public int? Id { get; set; }
        [Required(ErrorMessage = "نام کالا الزامی است.")] public string Name { get; set; } = "";
        public string? Slug { get; set; }
        public string? Description { get; set; }
        [Range(0, 1000000000, ErrorMessage = "قیمت معتبر نیست.")] public decimal Price { get; set; }
        [Range(0, 100, ErrorMessage = "درصد تخفیف باید بین ۰ و ۱۰۰ باشد.")] public decimal? DiscountPercent { get; set; }
        [Range(0, 1000000, ErrorMessage = "موجودی معتبر نیست.")] public int Stock { get; set; }
        public string? Sku { get; set; }
        public string? ImageUrl { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "دسته‌بندی را انتخاب کنید.")] public int CategoryId { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        var result = await mediator.Send(new GetAdminProductQuery(id));
        if (!result.IsSuccess || result.ResultBody is null)
        {
            TempData["Error"] = result.Message;
            return RedirectToPage("Index");
        }

        Fill(result.ResultBody);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(IFormFile? image)
    {
        var current = await mediator.Send(new GetAdminProductQuery(Input.Id));
        Categories = current.ResultBody?.Categories ?? [];
        if (!ModelState.IsValid)
            return Page();

        var imageUrl = Input.ImageUrl;
        if (image is { Length: > 0 })
        {
            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (image.Length > 2 * 1024 * 1024 || extension is not (".jpg" or ".jpeg" or ".png" or ".webp"))
            {
                ModelState.AddModelError(string.Empty, "تصویر باید jpg، png یا webp و حداکثر ۲ مگابایت باشد.");
                return Page();
            }

            var folder = Path.Combine(environment.WebRootPath, "uploads");
            Directory.CreateDirectory(folder);
            var fileName = $"{Guid.NewGuid():N}{extension}";
            await using var stream = System.IO.File.Create(Path.Combine(folder, fileName));
            await image.CopyToAsync(stream);
            imageUrl = $"/uploads/{fileName}";
        }

        var result = await mediator.Send(new SaveProductCommand(
            Input.Id, Input.Name, Input.Slug, Input.Description, Input.Price, Input.DiscountPercent,
            Input.Stock, Input.Sku, imageUrl, Input.CategoryId, Input.IsActive));

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "ذخیره کالا انجام نشد.");
            return Page();
        }

        TempData["Success"] = result.Message;
        return RedirectToPage("Index");
    }

    private void Fill(AdminProductEditDto product)
    {
        Categories = product.Categories;
        Input = new InputModel
        {
            Id = product.Id,
            Name = product.Name,
            Slug = product.Slug,
            Description = product.Description,
            Price = product.Price,
            DiscountPercent = product.DiscountPercent,
            Stock = product.Stock,
            Sku = product.Sku,
            ImageUrl = product.ImageUrl,
            CategoryId = product.CategoryId,
            IsActive = product.IsActive
        };
    }
}
