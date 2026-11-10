using Raya.Auth.Entities.Account;

namespace Domain.Entities.Users;

public class User : RayaAuthUser
{
    /// <summary>
    /// وضعیت
    /// </summary>
    public byte Status { get; set; } = 1;

    /// <summary>
    /// اخرین ورود
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    #region Relations
    public virtual ICollection<UserRole> UserRoles { get; set; }
        = new List<UserRole>();
    #endregion
}