using Buddham.Core.Entities.Public;
using Buddham.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buddham.Shared.Configurations.Public;

public class TangwhaConfiguration : IEntityTypeConfiguration<Tangwha>, IPublicConfiguration
{
    public void Configure(EntityTypeBuilder<Tangwha> builder)
    {
        builder.ToTable("tangwahs", SchemaTypes.Public);
        builder.HasKey(x => x.Id);

        // ── 기본 컬럼 ────────────────────────────────
        builder.Property(x => x.Id).UseIdentityByDefaultColumn();
        builder.Property(x => x.Title).IsRequired().HasMaxLength(500).HasColumnType(PgTypes.Text);
        builder.Property(x => x.ChineseTitle).IsRequired().HasMaxLength(500).HasColumnType(PgTypes.Text);
        builder.Property(x => x.UserId).IsRequired().HasColumnType(PgTypes.Uuid);

        // ── Full-Text Search용 tsvector (계산된 컬럼) ──
        builder.Property(x => x.SearchVector)
            .HasColumnType(PgTypes.TsVector)
            .HasConversion(EfConverters.TsVectorConverter) // 읽기 방향만 진짜로 의미 있음
            .HasComputedColumnSql(
                "setweight(to_tsvector('simple', coalesce(title, '')), 'A') || " +
                "setweight(to_tsvector('simple', coalesce(description, '')), 'B')",
                stored: true)
            // 핵심: "이 값은 DB가 Add/Update 시점마다 재계산"
            .ValueGeneratedOnAddOrUpdate()
            // EF가 이 프로퍼티를 저장 페이로드에서 아예 제외
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        // ── index ──────────────────────────────────
        builder.HasIndex(x => new { x.CreatedAt, x.Id })
            .IsDescending(true, true)
            .HasDatabaseName("ix_tangwha_created_at_id_desc");


        // ── GIN ───────────────────────────────────
        builder.HasIndex(x => x.SearchVector)
            .HasMethod(PgMethods.GIN)
            .HasDatabaseName("ix_tangwha_search_vector");

        // ── HNSW: 백터 유사도 검색, 코사인 유사도 ─────────
        builder.HasIndex(x => x.Embedding)
            .HasMethod(PgMethods.HNSW)
            .HasOperators("vector_cosine_ops")
            .HasDatabaseName("ix_tangwha_embedding");

        // ── UserId: 유저별 조회 최적화 ─────────────────
        builder.HasIndex(x => x.UserId).HasDatabaseName("ix_tangwha_user_id");
    }
}
