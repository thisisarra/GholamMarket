using Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Pinex.Domain.Entities.Users;
using Raya.Auth.DBContext;

namespace Infrastructure.DBContexts;

public partial class GholamContext : RayaAuthContext<User, Role, UserRole>
{
    public GholamContext
   (
   DbContextOptions<GholamContext> options
   ) : base(options)
    {
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IInfraAssemblyMarker).Assembly);
        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
