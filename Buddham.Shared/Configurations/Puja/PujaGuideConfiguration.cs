using Buddham.Core.Entities.Puja;
using Buddham.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NpgsqlTypes;
using Pgvector;

namespace Buddham.Shared.Configurations.Puja;

public class PujaGuideConfiguration : IEntityTypeConfiguration<PujaGuide>, IPujaConfiguration
{
    public void Configure(EntityTypeBuilder<PujaGuide> builder)
    {
        builder.ToTable("puja_guides", SchemaTypes.Puja);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName(ColumnNames.Id).HasColumnType(PgTypes.BigInt).ValueGeneratedOnAdd();

        builder.Property(x => x.CategoryId).HasColumnName("category_id").IsRequired();
        builder.Property(x => x.Title).HasColumnName("title").HasColumnType(PgTypes.Text).IsRequired();
        builder.Property(x => x.Summary).HasColumnName("summary").HasColumnType(PgTypes.Text).IsRequired(false);
        builder.Property(x => x.Content).HasColumnName("content").HasColumnType(PgTypes.Text).IsRequired(false);

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

        builder.HasIndex(x => x.CategoryId).HasDatabaseName("ix_puja_guides_category_id");
        builder.HasIndex(x => x.SearchVector).HasMethod("GIN").HasDatabaseName("ix_puja_guides_search_vector");
        builder.HasIndex(x => x.Embedding).HasMethod("hnsw").HasDatabaseName("ix_puja_guides_embedding").HasOperators("vector_cosine_ops");
    }
}
