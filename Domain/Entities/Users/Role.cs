using Pinex.Domain.Entities.Users;
using Raya.Auth.Entities.Account;

namespace Domain.Entities.Users;

public class Role : RayaAuthRoles
{
    /// <summary>
    /// توضیحات 
    /// </summary>
    public string? Description { get; set; }

    #region Relations
    public virtual ICollection<RolePermission>? RolePermissions { get; set; }
        = new List<RolePermission>();

    public virtual ICollection<UserRole>? UserRoles { get; set; }
        = new List<UserRole>();
    #endregion
}
