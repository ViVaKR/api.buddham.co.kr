using System.Reflection;
using Buddham.Core.Entities.Comment;
using Buddham.Core.Entities.Public;
using Buddham.Shared.Configurations;
using Buddham.Shared.Constants;
using Buddham.Shared.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Buddham.Shared.Data;

public class PublicDbContext(DbContextOptions<PublicDbContext> options) : DbContext(options)
{
    public DbSet<Guide> Guides => Set<Guide>();
    public DbSet<GuideComment> GuideComments => Set<GuideComment>();
    public DbSet<GuideCommentLike> GuideCommentLikes => Set<GuideCommentLike>();

    /// <summary>
    /// 파일데이터
    /// </summary>
    public DbSet<FileData> FileDatas => Set<FileData>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.RegisterModelCreating(SchemaTypes.Public);

        builder.ApplyConfigurationsFromAssembly(
            Assembly.GetExecutingAssembly(),
            type => type is { IsInterface: false, IsAbstract: false } &&
                    (typeof(IPublicConfiguration).IsAssignableFrom(type) ||
                    typeof(ICommentConfiguration).IsAssignableFrom(type))
        );
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        builder.Properties<Pgvector.Vector>().HaveColumnType(PgTypes.Vector);
        builder.Properties<DateTime>().HaveColumnType(PgTypes.TimestampTz);
        builder.Properties<DateTime?>().HaveColumnType(PgTypes.TimestampTz);
    }
}
