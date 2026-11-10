using Domain.Entities.Users;
using Microsoft.AspNetCore.Identity;

namespace Pinex.Domain.Entities.Users;

public class UserRole : IdentityUserRole<string>
{
    #region User
    public virtual User User { get; set; } = null!;
    #endregion

    #region Role
    public virtual Role Role { get; set; } = null!;
    #endregion
}