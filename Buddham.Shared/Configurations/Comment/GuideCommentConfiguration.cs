using Buddham.Core.Entities.Comment;
using Buddham.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buddham.Shared.Configurations.Comment;

public class GuideCommentConfiguration : IEntityTypeConfiguration<GuideComment>, ICommentConfiguration
{
    public void Configure(EntityTypeBuilder<GuideComment> builder)
    {
        builder.ToTable("guide_comments", SchemaTypes.Comment);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        // TargetId 는 GuideId 의 별칭이므로 컬럼 매핑 제외
        builder.Ignore(x => x.TargetId);

        // 컨벤션(snake_case, timestamptz)이 처리하지 못하는 것만 명시
        builder.Property(x => x.Pseudonym).HasMaxLength(100);
        builder.Property(x => x.MentionedUserName).HasMaxLength(100);
        builder.Property(x => x.Avatar).HasMaxLength(500);

        builder.Property(x => x.HapjangCount).HasDefaultValue(0);
        builder.Property(x => x.LotusCount).HasDefaultValue(0);
        builder.Property(x => x.MeritCount).HasDefaultValue(0);

        builder.Property(x => x.CreatedAt).IsRequired().HasDefaultValueSql("NOW()");

        // RootId -> guide_comments.id 자기 참조 (1단계 댓글은 root_id = null)
        // NoAction: 루트 + 대댓글을 한 문장(ExecuteDelete)으로 지울 때 문장 끝에서 검사하므로 안전.
        // (Restrict 는 즉시 검사라서 같은 문장 안에서도 FK 위반이 날 수 있음)
        builder.HasMany(x => x.Replies)
            .WithOne()
            .HasForeignKey(x => x.RootId) // root_id -> guide_comment.id
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false); // null 허용 (1단계 댓글은 rootId = null)

        // public.guides (스키마가 달라도 FK 는 정상 생성)
        builder.HasOne(x => x.Guide)
            .WithMany()
            .HasForeignKey(x => x.GuideId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Likes)
            .WithOne(x => x.Comment)
            .HasForeignKey(x => x.CommentId)
            .OnDelete(DeleteBehavior.Cascade);


        // -- 인덱스 --
        builder.HasIndex(x => new { x.GuideId, x.CreatedAt })
            .HasDatabaseName("ix_guide_comments_guide_id_create_at");

        builder.HasIndex(x => new { x.RootId, x.CreatedAt })
            .HasFilter("root_id IS NOT NULL")
            .HasDatabaseName("ix_guide_comments_root_id_created_at");

        builder.HasIndex(x => new { x.RootId, x.ParentId, x.CreatedAt })
            .HasFilter("root_id IS NOT NULL")
            .HasDatabaseName("ix_guide_comments_root_id_parent_id_created_at");

        builder.HasIndex(x => x.HapjangCount)
            .IsDescending(true)
            .HasDatabaseName("ix_guide_comments_hapjang_count");

        builder.HasIndex(x => x.LotusCount)
            .IsDescending(true)
            .HasDatabaseName("ix_guide_comments_lotus_count");

        builder.HasIndex(x => x.MeritCount)
            .IsDescending(true)
            .HasDatabaseName("ix_guide_comments_merit_count");
    }
}
