using Buddham.Core.Entities.Public;
using Buddham.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buddham.Shared.Configurations.Public;

public class MediaAttachmentConfiguration : IEntityTypeConfiguration<MediaAttachment>, IPublicConfiguration
{
    public void Configure(EntityTypeBuilder<MediaAttachment> builder)
    {
        builder.ToTable("media_attachments", SchemaTypes.Public);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName(ColumnNames.Id).HasColumnType(PgTypes.BigInt).ValueGeneratedOnAdd();

        builder.Property(x => x.TargetId).HasColumnName("target_id").IsRequired();
        builder.Ignore(x => x.TargetId);
        // TargetColumnPropertyName 패턴 유지 시 실제 컬럼명은 아래처럼 별도 매핑
        // ⚠️ ICommentTarget 패턴을 그대로 쓴다면 TargetId는 각 사용처에서 실제 컬럼(target_id)에 매핑되도록
        //    Ignore 없이 아래처럼 직접 컬럼으로 두는 편이 MediaAttachment에는 더 맞음 (다형성이라 파생타입이 없으므로)

        builder.Property(x => x.TargetType).HasColumnName("target_type").HasColumnType(PgTypes.Text).IsRequired();
        builder.Property(x => x.Type).HasColumnName("type").HasConversion<string>().HasColumnType(PgTypes.Text).IsRequired();
        builder.Property(x => x.Title).HasColumnName("title").HasColumnType(PgTypes.Text).IsRequired();
        builder.Property(x => x.Url).HasColumnName("url").HasColumnType(PgTypes.Text).IsRequired(false);
        builder.Property(x => x.ThumbnailUrl).HasColumnName("thumbnail_url").HasColumnType(PgTypes.Text).IsRequired(false);
        builder.Property(x => x.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);

        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.AuthorPseudonym).HasColumnName("author_pseudonym").HasColumnType(PgTypes.Text).IsRequired(false);

        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").IsRequired(false);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired(false);

        builder.Property(x => x.Metadata).HasColumnName("metadata").HasColumnType("jsonb");

        builder.HasIndex(x => new { x.TargetType, x.TargetId, x.SortOrder })
            .HasDatabaseName("ix_media_attachments_target");

        // JSONB 내부 특정 키로도 검색 가능하게 GIN 인덱스
        builder.HasIndex(x => x.Metadata)
            .HasMethod("GIN")
            .HasDatabaseName("ix_media_attachments_metadata");
    }
}