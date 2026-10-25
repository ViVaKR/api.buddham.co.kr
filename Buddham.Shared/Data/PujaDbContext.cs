using System.Reflection;
using Buddham.Core.Entities.Puja;
using Buddham.Shared.Configurations;
using Buddham.Shared.Constants;
using Buddham.Shared.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Buddham.Shared.Data;

public class PujaDbContext(DbContextOptions<PujaDbContext> options) : DbContext(options)
{
    public DbSet<PujaCategory> PujaCategories => Set<PujaCategory>();
    public DbSet<PujaGuide> PujaGuides => Set<PujaGuide>();
    public DbSet<PujaGuideStep> PujaGuideSteps => Set<PujaGuideStep>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.RegisterModelCreating(SchemaTypes.Puja);

        builder.ApplyConfigurationsFromAssembly(
            Assembly.GetExecutingAssembly(),
            type => typeof(IPujaConfiguration).IsAssignableFrom(type) && type is { IsInterface: false, IsAbstract: false }
        );
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        builder.Properties<Pgvector.Vector>().HaveColumnType(PgTypes.Vector);
        builder.Properties<DateTime>().HaveColumnType(PgTypes.TimestampTz);
        builder.Properties<DateTime?>().HaveColumnType(PgTypes.TimestampTz);
    }
}
