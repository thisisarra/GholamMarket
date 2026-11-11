using NibFramework.Model;

namespace Domain.Common;

internal static class EntityTime
{
    public static void Touch(this BaseEntity<int> entity)
        => entity.UpdatedAt = DateTime.UtcNow;
}
