using System.Reflection;
using Buddham.Core.Entities.Pitaka;
using Buddham.Shared.Configurations;
using Buddham.Shared.Constants;
using Buddham.Shared.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Buddham.Shared.Data;

public class PitakaDbContext(DbContextOptions<PitakaDbContext> options) : DbContext(options)
{
    public DbSet<PitakaCategory> PitakaCategories => Set<PitakaCategory>();
    public DbSet<PitakaScripture> PitakaScriptures => Set<PitakaScripture>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.RegisterModelCreating(SchemaTypes.Pitaka);

        builder.ApplyConfigurationsFromAssembly(
            Assembly.GetExecutingAssembly(),
            type => typeof(IPitakaConfiguration).IsAssignableFrom(type) && type is { IsInterface: false, IsAbstract: false }
        );
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        builder.Properties<Pgvector.Vector>().HaveColumnType(PgTypes.Vector);
        builder.Properties<DateTime>().HaveColumnType(PgTypes.TimestampTz);
        builder.Properties<DateTime?>().HaveColumnType(PgTypes.TimestampTz);
    }
}