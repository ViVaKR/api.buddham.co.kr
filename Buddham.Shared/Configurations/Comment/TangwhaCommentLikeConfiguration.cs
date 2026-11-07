using Buddham.Core.Entities.Comment;
using Buddham.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buddham.Shared.Configurations.Comment;

public class TangwhaCommentLikeConfiguration : IEntityTypeConfiguration<TangwhaCommentLike>, ICommentConfiguration
{
    public void Configure(EntityTypeBuilder<TangwhaCommentLike> builder)
    {
        builder.ToTable("tangwha_comment_likes", SchemaTypes.Comment);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.CreatedAt).IsRequired().HasDefaultValueSql("NOW()");

        builder.HasIndex(x => new { x.CommentId, x.UserId })
            .IsUnique()
            .HasDatabaseName("ix_tangwha_comment_likes_unique_comment_id_user_id");

        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("ix_tangwha_comment_likes_user_id");
    }
}
