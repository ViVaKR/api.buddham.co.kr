using Buddham.Core.Entities.Desana;
using Buddham.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NpgsqlTypes;
using Pgvector;

namespace Buddham.Shared.Configurations.Desana;

public class DesanaPostConfiguration : IEntityTypeConfiguration<DesanaPost>, IDesanaConfiguration
{
    public void Configure(EntityTypeBuilder<DesanaPost> builder)
    {
        builder.ToTable("desana_posts", SchemaTypes.Desana);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName(ColumnNames.Id).HasColumnType(PgTypes.BigInt).ValueGeneratedOnAdd();

        builder.Property(x => x.Type).HasColumnName("type").HasConversion<string>().HasColumnType(PgTypes.Text).IsRequired();

        builder.Property(x => x.Title).HasColumnName("title").HasColumnType(PgTypes.Text).IsRequired();
        builder.Property(x => x.Content).HasColumnName("content").HasColumnType(PgTypes.Text).IsRequired();
        builder.Property(x => x.Summary).HasColumnName("summary").HasColumnType(PgTypes.Text).IsRequired(false);

        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.AuthorPseudonym).HasColumnName("author_pseudonym").HasColumnType(PgTypes.Text).IsRequired(false);
        builder.Property(x => x.AuthorName).HasColumnName("author_name").HasColumnType(PgTypes.Text).IsRequired(false);

        builder.Property(x => x.PitakaScriptureId).HasColumnName("pitaka_scripture_id").IsRequired(false);

        builder.Property(x => x.ParentId).HasColumnName("parent_id").IsRequired(false);
        builder.Property(x => x.EpisodeNumber).HasColumnName("episode_number").IsRequired(false);
        builder.Property(x => x.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);

        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasColumnType(PgTypes.Text).IsRequired();
        builder.Property(x => x.PublishedAt).HasColumnName("published_at").IsRequired(false);

        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").IsRequired(false);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired(false);

        // 시리즈 자기참조 (부모 회차 있으면 실수 삭제 방지)
        builder.HasOne(x => x.Parent)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        var tsVectorConverter = new ValueConverter<string?, NpgsqlTsVector?>(
            v => v == null ? null : (NpgsqlTsVector?)(object)v,
            v => v == null ? null : v.ToString());

        builder.Property(x => x.SearchVector)
            .HasColumnType("tsvector")
            .HasConversion(tsVectorConverter)
            .HasComputedColumnSql(
                "setweight(to_tsvector('simple', coalesce(title, '')), 'A') || " +
                "setweight(to_tsvector('simple', coalesce(summary, '')), 'B') || " +
                "setweight(to_tsvector('simple', coalesce(content, '')), 'C')",
                stored: true)
            .ValueGeneratedOnAddOrUpdate()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        var vectorConverter = new ValueConverter<float[]?, Vector?>(
            v => v == null ? null : new Vector(v),
            v => v == null ? null : v.ToArray());

        builder.Property(x => x.Embedding)
            .HasColumnType("vector(1024)")
            .HasConversion(vectorConverter);

        builder.HasIndex(x => x.PitakaScriptureId).HasDatabaseName("ix_desana_posts_pitaka_scripture_id");
        builder.HasIndex(x => new { x.ParentId, x.EpisodeNumber }).HasDatabaseName("ix_desana_posts_parent_id_episode_number");
        builder.HasIndex(x => new { x.Type, x.Status, x.PublishedAt }).HasDatabaseName("ix_desana_posts_type_status_published_at");
        builder.HasIndex(x => x.SearchVector).HasMethod("GIN").HasDatabaseName("ix_desana_posts_search_vector");
        builder.HasIndex(x => x.Embedding).HasMethod("hnsw").HasDatabaseName("ix_desana_posts_embedding").HasOperators("vector_cosine_ops");
    }
}