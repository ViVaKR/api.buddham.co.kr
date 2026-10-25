using Buddham.Core.Entities.Public;
using Buddham.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NpgsqlTypes;
using Pgvector;

namespace Buddham.Shared.Configurations.Public;

public class DharmaTermConfiguration : IEntityTypeConfiguration<DharmaTerm>, IPublicConfiguration
{
    public void Configure(EntityTypeBuilder<DharmaTerm> builder)
    {
        builder.ToTable("dharma_terms", SchemaTypes.Public);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName(ColumnNames.Id).HasColumnType(PgTypes.BigInt).ValueGeneratedOnAdd();

        builder.Property(x => x.TermKo).HasColumnName("term_ko").HasColumnType(PgTypes.Text).IsRequired();
        builder.Property(x => x.TermSanskrit).HasColumnName("term_sanskrit").HasColumnType(PgTypes.Text).IsRequired(false);
        builder.Property(x => x.TermHanja).HasColumnName("term_hanja").HasColumnType(PgTypes.Text).IsRequired(false);
        builder.Property(x => x.TermEnglish).HasColumnName("term_english").HasColumnType(PgTypes.Text).IsRequired(false);
        builder.Property(x => x.Definition).HasColumnName("definition").HasColumnType(PgTypes.Text).IsRequired();
        builder.Property(x => x.Etymology).HasColumnName("etymology").HasColumnType(PgTypes.Text).IsRequired(false);
        builder.Property(x => x.Example).HasColumnName("example").HasColumnType(PgTypes.Text).IsRequired(false);

        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").IsRequired(false);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired(false);

        // ==================================================================================
        // ── 1. tsvector: 한글/한자/영문/산스크리트 용어 + 뜻풀이 전문검색(Full-Text Search) ──
        // ==================================================================================

        builder.Property(x => x.SearchVector)
            .HasColumnType(PgTypes.TsVector)
            .HasConversion(EfConverters.TsVectorConverter)
            .HasComputedColumnSql(
                "setweight(to_tsvector('simple', coalesce(term_ko, '')), 'A') || " +
                "setweight(to_tsvector('simple', coalesce(term_sanskrit, '') || ' ' || coalesce(term_hanja, '') || ' ' || coalesce(term_english, '')), 'B') || " +
                "setweight(to_tsvector('simple', coalesce(definition, '')), 'C')",
                stored: true)
            .ValueGeneratedOnAddOrUpdate()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        // ==================================================================================
        // ── 2. vector: 의미론적 검색 (예: "색즉시공"과 뜻이 비슷한 다른 용어 탐색 - pgvector) ──
        // ==================================================================================

        builder.Property(x => x.Embedding)
            .HasColumnType("vector(1024)")
            .HasConversion(EfConverters.VectorConverter);

        // ── 인덱스 ──
        builder.HasIndex(x => x.TermKo).HasDatabaseName("ix_dharma_terms_term_ko").IsUnique();

        // GIN
        builder.HasIndex(x => x.SearchVector)
            .HasMethod(PgMethods.GIN)
            .HasDatabaseName("ix_dharma_terms_search_vector");

        // HNSW
        builder.HasIndex(x => x.Embedding)
            .HasMethod(PgMethods.HNSW)
            .HasOperators("vector_cosine_ops")
            .HasDatabaseName("ix_dharma_terms_embedding");
    }
}
