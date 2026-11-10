using NibFramework.Model;

namespace Pinex.Domain.Entities.Users;

public class Permission : BaseEntity<int>
{
    /// <summary>
    /// کد
    /// </summary>
    public required string Code { get; set; }

    /// <summary>
    /// نام
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// توضیحات
    /// </summary>
    public string? Description { get; set; }

    #region Relations
    public ICollection<RolePermission>? RolePermissions { get; set; }
    #endregion
}