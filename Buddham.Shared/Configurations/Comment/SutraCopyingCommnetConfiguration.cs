using Buddham.Core.Entities.Comment;
using Buddham.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buddham.Shared.Configurations.Comment;

public class SutraCopyingConfiguration : IEntityTypeConfiguration<SutraCopyingComment>, ICommentConfiguration
{
    public void Configure(EntityTypeBuilder<SutraCopyingComment> builder)
    {
        builder.ToTable("sutra_copying_comments", SchemaTypes.Comment);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Ignore(x => x.TargetId);

        // 컨벤션(snake_case, timestamptz)이 처리하지 못하는 것만 명시
        builder.Property(x => x.Pseudonym).HasMaxLength(100);
        builder.Property(x => x.MentionedUserName).HasMaxLength(100);
        builder.Property(x => x.Avatar).HasMaxLength(500);

        builder.Property(x => x.HapjangCount).HasDefaultValue(0);
        builder.Property(x => x.LotusCount).HasDefaultValue(0);
        builder.Property(x => x.MeritCount).HasDefaultValue(0);

        builder.Property(x => x.CreatedAt).IsRequired().HasDefaultValueSql("NOW()");

        builder.HasMany(x => x.Replies)
            .WithOne()
            .HasForeignKey(x => x.RootId) // root_id -> guide_comment.id
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false); // null 허용 (1단계 댓글은 rootId = null)

        builder.HasOne(x => x.SutraCopying)
            .WithMany()
            .HasForeignKey(x => x.SutraCopyingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Likes)
            .WithOne(x => x.Comment)
            .HasForeignKey(x => x.CommentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.SutraCopyingId, x.CreatedAt })
            .HasDatabaseName("ix_sutra_copying_comments_sutra_copying_id_create_at");

        builder.HasIndex(x => new { x.RootId, x.CreatedAt })
            .HasFilter("root_id IS NOT NULL")
            .HasDatabaseName("ix_sutra_copying_comments_root_id_created_at");

        builder.HasIndex(x => new { x.RootId, x.ParentId, x.CreatedAt })
            .HasFilter("root_id IS NOT NULL")
            .HasDatabaseName("ix_sutra_copying_comments_root_id_parent_id_created_at");

        builder.HasIndex(x => x.HapjangCount)
            .IsDescending(true)
            .HasDatabaseName("ix_sutra_copying_comments_hapjang_count");

        builder.HasIndex(x => x.LotusCount)
            .IsDescending(true)
            .HasDatabaseName("ix_sutra_copying_comments_lotus_count");

        builder.HasIndex(x => x.MeritCount)
            .IsDescending(true)
            .HasDatabaseName("ix_sutra_copying_comments_merit_count");
    }
}
