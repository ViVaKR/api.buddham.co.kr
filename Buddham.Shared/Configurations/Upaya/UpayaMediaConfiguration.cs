using Buddham.Core.Entities.Upaya;
using Buddham.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NpgsqlTypes;
using Pgvector;

namespace Buddham.Shared.Configurations.Upaya;

public class UpayaMediaConfiguration : IEntityTypeConfiguration<UpayaMedia>, IUpayaConfiguration
{
    public void Configure(EntityTypeBuilder<UpayaMedia> builder)
    {
        builder.ToTable("upaya_media", SchemaTypes.Upaya);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName(ColumnNames.Id).HasColumnType(PgTypes.BigInt).ValueGeneratedOnAdd();

        builder.Property(x => x.CategoryId).HasColumnName("category_id").IsRequired();
        builder.Property(x => x.Type).HasColumnName("type").HasConversion<string>().HasColumnType(PgTypes.Text).IsRequired();

        builder.Property(x => x.Title).HasColumnName("title").HasColumnType(PgTypes.Text).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasColumnType(PgTypes.Text).IsRequired(false);

        builder.Property(x => x.FileDataId).HasColumnName("file_data_id").IsRequired(false);

        builder.Property(x => x.ExternalUrl).HasColumnName("external_url").HasColumnType(PgTypes.Text).IsRequired(false);
        builder.Property(x => x.YoutubeVideoId).HasColumnName("youtube_video_id").HasColumnType(PgTypes.Text).IsRequired(false);
        builder.Property(x => x.ChannelName).HasColumnName("channel_name").HasColumnType(PgTypes.Text).IsRequired(false);

        builder.Property(x => x.ThumbnailUrl).HasColumnName("thumbnail_url").HasColumnType(PgTypes.Text).IsRequired(false);
        builder.Property(x => x.DurationSeconds).HasColumnName("duration_seconds").IsRequired(false);

        builder.Property(x => x.Creator).HasColumnName("creator").HasColumnType(PgTypes.Text).IsRequired(false);
        builder.Property(x => x.Era).HasColumnName("era").HasColumnType(PgTypes.Text).IsRequired(false);

        builder.Property(x => x.Metadata).HasColumnName("metadata").HasColumnType("jsonb").IsRequired(false);

        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.AuthorPseudonym).HasColumnName("author_pseudonym").HasColumnType(PgTypes.Text).IsRequired(false);

        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").IsRequired(false);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired(false);

        builder.HasOne(x => x.Category)
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        var tsVectorConverter = new ValueConverter<string?, NpgsqlTsVector?>(
            v => v == null ? null : (NpgsqlTsVector?)(object)v,
            v => v == null ? null : v.ToString());

        builder.Property(x => x.SearchVector)
            .HasColumnType("tsvector")
            .HasConversion(tsVectorConverter)
            .HasComputedColumnSql(
                "setweight(to_tsvector('simple', coalesce(title, '')), 'A') || " +
                "setweight(to_tsvector('simple', coalesce(description, '')), 'B')",
                stored: true)
            .ValueGeneratedOnAddOrUpdate()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        var vectorConverter = new ValueConverter<float[]?, Vector?>(
            v => v == null ? null : new Vector(v),
            v => v == null ? null : v.ToArray());

        builder.Property(x => x.Embedding)
            .HasColumnType("vector(1024)")
            .HasConversion(vectorConverter);

        builder.HasIndex(x => x.CategoryId).HasDatabaseName("ix_upaya_media_category_id");
        builder.HasIndex(x => x.Type).HasDatabaseName("ix_upaya_media_type");
        builder.HasIndex(x => x.FileDataId).HasDatabaseName("ix_upaya_media_file_data_id");

        // 같은 유튜브 영상 중복 등록 방지
        builder.HasIndex(x => x.YoutubeVideoId)
            .IsUnique()
            .HasFilter("youtube_video_id IS NOT NULL")
            .HasDatabaseName("ix_upaya_media_youtube_video_id");

        builder.HasIndex(x => x.Metadata).HasMethod("GIN").HasDatabaseName("ix_upaya_media_metadata");
        builder.HasIndex(x => x.SearchVector).HasMethod("GIN").HasDatabaseName("ix_upaya_media_search_vector");
        builder.HasIndex(x => x.Embedding).HasMethod("hnsw").HasDatabaseName("ix_upaya_media_embedding").HasOperators("vector_cosine_ops");
    }
}