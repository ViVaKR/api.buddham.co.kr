using Buddham.Core.Entities.Public;
using Buddham.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NpgsqlTypes;
using Pgvector;

namespace Buddham.Shared.Configurations.Public;

public class GuideConfiguration : IEntityTypeConfiguration<Guide>, IPublicConfiguration
{
    public void Configure(EntityTypeBuilder<Guide> builder)
    {
        builder.ToTable("guides", SchemaTypes.Public);
        builder.HasKey(p => p.Id);

        // ── 기본 컬럼 ────────────────────────────────
        builder.Property(x => x.Id).UseIdentityByDefaultColumn();
        builder.Property(x => x.Title).IsRequired().HasMaxLength(500).HasColumnType(PgTypes.Text);
        builder.Property(x => x.Content).HasColumnType(PgTypes.Text);
        builder.Property(x => x.UserId).IsRequired().HasColumnType(PgTypes.Uuid);

        // ── tsvector 계산 컬럼 (순혈주의 수호대 2탄 🛡️) ───────────────────────
        // ── 1. Full-Text Search용 tsvector (계산된 컬럼) ──
        builder.Property(x => x.SearchVector)
            .HasColumnType(PgTypes.TsVector)
            .HasConversion(EfConverters.TsVectorConverter) // 읽기 방향만 진짜로 의미 있음
            .HasComputedColumnSql(
                "setweight(to_tsvector('simple', coalesce(title, '')), 'A') || " +
                "setweight(to_tsvector('simple', coalesce(content, '')), 'B')",
                stored: true)
            // ✅ 핵심: "이 값은 DB가 Add/Update 시점마다 재계산해"
            .ValueGeneratedOnAddOrUpdate()
            // ✅ EF가 이 프로퍼티를 저장 페이로드에서 아예 제외
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        // vector
        builder.Property(x => x.Embedding)
            .HasColumnType("vector(1024)")
            .HasConversion(EfConverters.VectorConverter);

        // ── 인덱스 ───────────────────────────────────

        builder.HasIndex(x => new { x.CreatedAt, x.Id })
            .IsDescending(true, true)
            .HasDatabaseName("ix_guides_created_at_id_desc");

        // GIN
        builder.HasIndex(x => x.SearchVector)
            .HasMethod("GIN")
            .HasDatabaseName("ix_guides_search_vector");

        // HNSW 인덱스: 벡터 유사도 검색용 (코사인 유사도)
        builder.HasIndex(x => x.Embedding)
            .HasMethod("hnsw")
            .HasOperators("vector_cosine_ops")
            .HasDatabaseName("ix_guides_embedding");

        // UserId 인덱스 (유저별 조회 최적화용)
        builder.HasIndex(x => x.UserId).HasDatabaseName("ix_guides_user_id");
    }
}
