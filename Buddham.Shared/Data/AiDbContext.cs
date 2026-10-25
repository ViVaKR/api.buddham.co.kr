using System.Reflection;
using Buddham.Core.Entities.Ai;
using Buddham.Shared.Configurations;
using Buddham.Shared.Constants;
using Buddham.Shared.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Buddham.Shared.Data;

public class AiDbContext(DbContextOptions<AiDbContext> options) : DbContext(options)
{
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<AiArchive> AiArchives => Set<AiArchive>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.RegisterModelCreating(SchemaTypes.Ai);

        // 👈 추가
        builder.ApplyConfigurationsFromAssembly(
            Assembly.GetExecutingAssembly(),
            type => typeof(IAiConfiguration).IsAssignableFrom(type)
                    && type is { IsInterface: false, IsAbstract: false }
        );
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<Pgvector.Vector>().HaveColumnType("vector");
    }
}