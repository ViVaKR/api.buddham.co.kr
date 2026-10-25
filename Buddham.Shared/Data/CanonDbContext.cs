using System.Reflection;
using Buddham.Core.Entities.Canon;
using Buddham.Shared.Configurations;
using Buddham.Shared.Constants;
using Buddham.Shared.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Buddham.Shared.Data;

public class CanonDbContext(DbContextOptions<CanonDbContext> options) : DbContext(options)
{
    public DbSet<CanonCollection> CanonCollections => Set<CanonCollection>();
    public DbSet<CanonWork> CanonWorks => Set<CanonWork>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.RegisterModelCreating(SchemaTypes.Canon);

        builder.ApplyConfigurationsFromAssembly(
            Assembly.GetExecutingAssembly(),
            type => typeof(ICanonConfiguration).IsAssignableFrom(type) && type is { IsInterface: false, IsAbstract: false }
        );
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        builder.Properties<Pgvector.Vector>().HaveColumnType(PgTypes.Vector);
        builder.Properties<DateTime>().HaveColumnType(PgTypes.TimestampTz);
        builder.Properties<DateTime?>().HaveColumnType(PgTypes.TimestampTz);
    }
}
