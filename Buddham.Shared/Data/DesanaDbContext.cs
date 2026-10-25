using System.Reflection;
using Buddham.Core.Entities.Desana;
using Buddham.Shared.Configurations;
using Buddham.Shared.Constants;
using Buddham.Shared.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Buddham.Shared.Data;

public class DesanaDbContext(DbContextOptions<DesanaDbContext> options) : DbContext(options)
{
    public DbSet<DesanaPost> DesanaPosts => Set<DesanaPost>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.RegisterModelCreating(SchemaTypes.Desana);

        builder.ApplyConfigurationsFromAssembly(
            Assembly.GetExecutingAssembly(),
            type => typeof(IDesanaConfiguration).IsAssignableFrom(type) && type is { IsInterface: false, IsAbstract: false }
        );
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        builder.Properties<Pgvector.Vector>().HaveColumnType(PgTypes.Vector);
        builder.Properties<DateTime>().HaveColumnType(PgTypes.TimestampTz);
        builder.Properties<DateTime?>().HaveColumnType(PgTypes.TimestampTz);
    }
}