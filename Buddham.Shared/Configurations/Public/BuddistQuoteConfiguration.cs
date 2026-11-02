using Buddham.Core.Entities.Public;
using Buddham.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buddham.Shared.Configurations.Public;

public class BuddistQuoteConfiguration : IEntityTypeConfiguration<BuddistQuote>, IPublicConfiguration
{
    public void Configure(EntityTypeBuilder<BuddistQuote> builder)
    {
        builder.ToTable("buddist_quotes", SchemaTypes.Public);
        builder.HasKey(x => x.Id);

        // ── 기본 컬럼 ────────────────────────────────
        builder.Property(x => x.Id).UseIdentityByDefaultColumn();
        builder.Property(x => x.Quote).IsRequired().HasMaxLength(1024).HasColumnType(PgTypes.Text);
        builder.Property(x => x.Source).HasMaxLength(50).HasColumnType(PgTypes.Text);
        builder.Property(x => x.UserId).IsRequired().HasColumnType(PgTypes.Uuid);

        // ── 1. Full-Text Search용 tsvector (계산된 컬럼) ──
        builder.Property(x => x.SearchVector)
            .HasColumnType(PgTypes.TsVector)
            .HasConversion(EfConverters.TsVectorConverter) // 읽기 방향만 진짜로 의미 있음
            .HasComputedColumnSql(
                "setweight(to_tsvector('simple', coalesce(quote, '')), 'A') || " +
                "setweight(to_tsvector('simple', coalesce(source, '')), 'B')",
                stored: true)
            // 핵심: "이 값은 DB가 Add/Update 시점마다 재계산"
            .ValueGeneratedOnAddOrUpdate()
            // EF가 이 프로퍼티를 저장 페이로드에서 아예 제외
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        // ── vector ────────────────────────────────
        builder.Property(x => x.Embedding)
           .HasColumnType("vector(1024")
           .HasConversion(EfConverters.VectorConverter);

        // ── index ────────────────────────────────
        builder.HasIndex(x => new { x.CreatedAt, x.Id })
            .IsDescending(true, true)
            .HasDatabaseName("ix_buddist_quotes_created_at_id_desc");

        // ── GIN ──────────────────────────────────
        builder.HasIndex(x => x.SearchVector)
            .HasMethod("GIN")
            .HasDatabaseName("ix_buddist_quotes_vector");

        // ── HNSW 벡터 유사도, 코사이 유사도 검색 ─────────────
        builder.HasIndex(x => x.Embedding)
            .HasMethod("hnsw")
            .HasOperators("vector_cosine_ops")
            .HasDatabaseName("ix_buddhist_quotes_embedding");

    }
}
