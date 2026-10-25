using System.Reflection;
using Buddham.Core.Entities.Comment;
using Buddham.Shared.Configurations;
using Buddham.Shared.Constants;
using Buddham.Shared.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Buddham.Shared.Data;

public class CommentDbContext(DbContextOptions<CommentDbContext> options)
    : DbContext(options)
{
    // public DbSet<GuideComment> GuideComments => Set<GuideComment>();
    // public DbSet<GuideCommentLike> GuideCommentLikes => Set<GuideCommentLike>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.RegisterModelCreating(SchemaTypes.Comment);

        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly(),
            type => typeof(ICommentConfiguration).IsAssignableFrom(type)
                    && type is { IsInterface: false, IsAbstract: false });
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        builder.Properties<Pgvector.Vector>().HaveColumnType(PgTypes.Vector);
        builder.Properties<DateTime>().HaveColumnType(PgTypes.TimestampTz);
        builder.Properties<DateTime?>().HaveColumnType(PgTypes.TimestampTz);
    }
}
