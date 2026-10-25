using System.Reflection;
using Buddham.Core.Entities.Auth;
using Buddham.Shared.Configurations;
using Buddham.Shared.Constants;
using Buddham.Shared.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Buddham.Shared.Data;

public class BuddhamDbContext(DbContextOptions<BuddhamDbContext> options) : IdentityDbContext<
    ApplicationUser,
    ApplicationRole,
    Guid,
    IdentityUserClaim<Guid>,
    ApplicationUserRole,
    IdentityUserLogin<Guid>,
    IdentityRoleClaim<Guid>,
    IdentityUserToken<Guid>>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.RegisterModelCreating(SchemaTypes.Auth);
        builder.Entity<ApplicationUser>().ToTable(TableNames.Users, SchemaTypes.Auth);
        builder.Entity<ApplicationRole>().ToTable(TableNames.Roles, SchemaTypes.Auth);
        builder.Entity<ApplicationUserRole>().ToTable(TableNames.UserRoles, SchemaTypes.Auth);
        builder.Entity<IdentityUserClaim<Guid>>().ToTable(TableNames.UserClaims, SchemaTypes.Auth);
        builder.Entity<IdentityUserLogin<Guid>>().ToTable(TableNames.UserLogins, SchemaTypes.Auth);
        builder.Entity<IdentityUserToken<Guid>>().ToTable(TableNames.UserTokens, SchemaTypes.Auth);
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable(TableNames.RoleClaims, SchemaTypes.Auth);

        builder.ApplyConfigurationsFromAssembly(
            Assembly.GetExecutingAssembly(),
            type => typeof(IAuthConfiguration).IsAssignableFrom(type) && type is { IsInterface: false, IsAbstract: false }
        );
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        builder.Properties<Pgvector.Vector>().HaveColumnType(PgTypes.Vector);
        builder.Properties<DateTime>().HaveColumnType(PgTypes.TimestampTz);
        builder.Properties<DateTime?>().HaveColumnType(PgTypes.TimestampTz);
    }
}
