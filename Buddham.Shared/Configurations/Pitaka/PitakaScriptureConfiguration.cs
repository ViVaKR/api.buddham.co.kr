using Buddham.Core.Entities.Pitaka;
using Buddham.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NpgsqlTypes;
using Pgvector;

namespace Buddham.Shared.Configurations.Pitaka;

public class PitakaScriptureConfiguration : IEntityTypeConfiguration<PitakaScripture>, IPitakaConfiguration
{
    public void Configure(EntityTypeBuilder<PitakaScripture> builder)
    {
        builder.ToTable("pitaka_scriptures", SchemaTypes.Pitaka);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName(ColumnNames.Id).HasColumnType(PgTypes.BigInt).ValueGeneratedOnAdd();
        builder.Property(x => x.CategoryId).HasColumnName("category_id").IsRequired();
        builder.Property(x => x.TitleKo).HasColumnName("title_ko").HasColumnType(PgTypes.Text).IsRequired();
        builder.Property(x => x.TitlePali).HasColumnName("title_pali").HasColumnType(PgTypes.Text).IsRequired(false);
        builder.Property(x => x.Content).HasColumnName("content").HasColumnType(PgTypes.Text).IsRequired();
        builder.Property(x => x.Commentary).HasColumnName("commentary").HasColumnType(PgTypes.Text).IsRequired(false);

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
                "setweight(to_tsvector('simple', coalesce(title_ko, '')), 'A') || " +
                "setweight(to_tsvector('simple', coalesce(content, '')), 'B')",
                stored: true)
            .ValueGeneratedOnAddOrUpdate()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        var vectorConverter = new ValueConverter<float[]?, Vector?>(
            v => v == null ? null : new Vector(v),
            v => v == null ? null : v.ToArray());

        builder.Property(x => x.Embedding)
            .HasColumnType("vector(1024)")
            .HasConversion(vectorConverter);

        builder.HasIndex(x => x.CategoryId).HasDatabaseName("ix_pitaka_scriptures_category_id");
        builder.HasIndex(x => x.SearchVector).HasMethod("GIN").HasDatabaseName("ix_pitaka_scriptures_search_vector");
        builder.HasIndex(x => x.Embedding).HasMethod("hnsw").HasDatabaseName("ix_pitaka_scriptures_embedding").HasOperators("vector_cosine_ops");
    }
}