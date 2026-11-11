using Domain.Common;
using NibFramework.Model;

namespace Domain.Entities.Catalog;

public class Category : BaseEntity<int>
{
    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public string? Description { get; private set; }
    public int? ParentId { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; }

    #region Relations
    public virtual Category? Parent { get; private set; }
    public virtual ICollection<Category> Children { get; private set; } = new List<Category>();
    public virtual ICollection<Product> Products { get; private set; } = new List<Product>();
    #endregion

    protected Category()
    {
    }

    public Category(string name, string slug, string? description = null, int displayOrder = 0)
    {
        SetDetails(name, slug, description, displayOrder, isActive: true);
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string name, string slug, string? description, int displayOrder, bool isActive)
    {
        SetDetails(name, slug, description, displayOrder, isActive);
        this.Touch();
    }

    public Category AddSubCategory(string name, string slug, string? description = null, int displayOrder = 0)
    {
        var child = new Category(name, slug, description, displayOrder);
        child.AssignParent(this);
        Children.Add(child);
        return child;
    }

    public void MoveUnder(Category? parent) => AssignParent(parent);

    public IReadOnlyList<Product> GetProducts() => Products.ToList();

    private void AssignParent(Category? parent)
    {
        var cursor = parent;
        while (cursor is not null)
        {
            if (ReferenceEquals(cursor, this) || (Id != 0 && cursor.Id == Id))
                throw new DomainException("A category cannot be placed under itself.");

            cursor = cursor.Parent;
        }

        Parent = parent;
        ParentId = parent is null || parent.Id == 0 ? null : parent.Id;
        this.Touch();
    }

    private void SetDetails(string name, string slug, string? description, int displayOrder, bool isActive)
    {
        if (displayOrder < 0)
            throw new ArgumentOutOfRangeException(nameof(displayOrder), "Display order cannot be negative.");

        Name = Guard.Required(name, nameof(name), FieldLengths.Name);
        Slug = Guard.Slug(slug, nameof(slug));
        Description = Guard.Optional(description, nameof(description), FieldLengths.Description);
        DisplayOrder = displayOrder;
        IsActive = isActive;
    }
}
