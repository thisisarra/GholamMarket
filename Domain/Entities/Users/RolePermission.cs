namespace Pinex.Domain.Entities.Users;

public class RolePermission
{
    #region Role
    public Role? Role { get; set; }
    public string RoleId { get; set; } = null!;
    #endregion

    #region Permission
    public Permission? Permission { get; set; }
    public int PermissionId { get; set; }
    #endregion
}