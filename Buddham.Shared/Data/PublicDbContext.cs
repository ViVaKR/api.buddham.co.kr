using System.Reflection;
using Buddham.Core.Entities.Code;
using Buddham.Core.Entities.Comment;
using Buddham.Core.Entities.Public;
using Buddham.Shared.Configurations;
using Buddham.Shared.Constants;
using Buddham.Shared.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Buddham.Shared.Data;

public class PublicDbContext(DbContextOptions<PublicDbContext> options) : DbContext(options)
{
    /// <summary>
    /// 가이드
    /// </summary>
    public DbSet<Guide> Guides => Set<Guide>();
    public DbSet<GuideComment> GuideComments => Set<GuideComment>();
    public DbSet<GuideCommentLike> GuideCommentLikes => Set<GuideCommentLike>();

    // 매일 사경
    public DbSet<SutraCopying> SutraCopyings => Set<SutraCopying>();
    public DbSet<SutraCopyingComment> SutraCopyingComments => Set<SutraCopyingComment>();
    public DbSet<SutraCopyingCommentLike> SutraCopyingCommentLikes => Set<SutraCopyingCommentLike>();

    /// <summary>
    /// 불교용어
    /// </summary>
    public DbSet<DharmaTerm> DharmaTerms => Set<DharmaTerm>();

    /// <summary>
    /// 첨부 미디어
    /// </summary>
    public DbSet<MediaAttachment> MediaAttachments => Set<MediaAttachment>();

    /// <summary>
    /// 파일데이터
    /// </summary>
    public DbSet<FileData> FileDatas => Set<FileData>();

    /// <summary>
    /// 어셈블리 시스콜
    /// </summary>
    public DbSet<AppleSyscall> AppleSyscalls => Set<AppleSyscall>();

    /// <summary>
    /// 코드 조각
    /// </summary>
    public DbSet<Code> Codes => Set<Code>();


    /// <summary>
    /// Model Creating
    /// </summary>
    /// <param name="builder"></param>
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
        // builder.ApplyConfigurationsFromAssembly(
        //     Assembly.GetExecutingAssembly(),
        //     type => typeof(IPublicConfiguration).IsAssignableFrom(type) && type is { IsInterface: false, IsAbstract: false }
        // );
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        builder.Properties<Pgvector.Vector>().HaveColumnType(PgTypes.Vector);
        builder.Properties<DateTime>().HaveColumnType(PgTypes.TimestampTz);
        builder.Properties<DateTime?>().HaveColumnType(PgTypes.TimestampTz);
    }
}
