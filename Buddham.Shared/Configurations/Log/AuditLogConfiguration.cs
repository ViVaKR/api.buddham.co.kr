using Buddham.Core.Entities.Log;
using Buddham.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NpgsqlTypes;
using Pgvector;

namespace Buddham.Shared.Configurations.Log;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>, ILogConfiguration
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs", SchemaTypes.Log);
        builder.HasKey(x => x.Id);

        // ── 컬럼 ────────────────────────────────────
        builder.Property(x => x.Category).HasMaxLength(100).HasColumnType(PgTypes.Text);
        builder.Property(x => x.UserEmail).IsRequired().HasMaxLength(256).HasColumnType("varchar(256)");
        builder.Property(x => x.ActionType).IsRequired().HasMaxLength(100).HasColumnType("varchar(100)");
        builder.Property(x => x.IpAddress).HasMaxLength(100).HasColumnType("varchar(100)");

        // 👑 모델에 있던 UserAgent도 안전하게 컬럼 지정을 해줍니다 (200자 -> 500자 확장)
        builder.Property(x => x.UserAgent).HasMaxLength(500).HasColumnType("varchar(500)");

        builder.Property(x => x.RequestPath).HasMaxLength(500).HasColumnType("varchar(500)");
        builder.Property(x => x.Message).HasColumnType("text");
        builder.Property(x => x.CreatedAt).IsRequired().HasDefaultValueSql("NOW()");
        builder.Property(x => x.OldValue).HasColumnType("jsonb");
        builder.Property(x => x.NewValue).HasColumnType("jsonb");

        // ── tsvector 계산 컬럼 (순혈주의 수호대 2탄 🛡️) ───────────────────────
        // C# string?과 DB tsvector(NpgsqlTsVector) 간의 중개 변환기
        // 💡 Parse()를 쓰지 않고 경고를 완벽히 회피하는 변환기
        var tsVectorConverter = new ValueConverter<string?, NpgsqlTsVector?>(
            s => null, // DB에 저장할 때는 null로 던짐 (어차피 DB가 자동 계산함)
            v => v == null ? null : v.ToString() // DB에서 읽어올 때만 string으로 변환
        );

        // ── tsvector 계산 컬럼 ───────────────────────
        builder.Property(x => x.SearchVector)
            .HasColumnType("tsvector")
            .HasConversion(tsVectorConverter)
            .HasComputedColumnSql(
                "setweight(to_tsvector('simple', coalesce(\"category\", '')), 'A') || " +
                "setweight(to_tsvector('simple', coalesce(\"action_type\", '')), 'B') || " +
                "setweight(to_tsvector('simple', coalesce(\"ip_address\", '')), 'B') || " +
                "setweight(to_tsvector('simple', coalesce(\"user_email\", '')), 'C') || " +
                "setweight(jsonb_to_tsvector('simple', coalesce(\"new_value\", '{}'::jsonb), '[\"string\"]'), 'C') || " +
                "setweight(to_tsvector('simple', coalesce(\"http_method\", '')), 'D')",
                stored: true);

        // ── Vector 컬럼 (순혈주의 수호대 🛡️) ──────────────────────────────
        // Core의 float[]를 DB의 vector(1024)로 중개해주는 변환기 정의
        var vectorConverter = new ValueConverter<float[]?, Vector?>(
            // C# float[] -> DB Vector
            v => v == null ? null : new Vector(v),
            // DB Vector -> C# float[]
            v => v == null ? null : v.ToArray()
        );

        // ── Vector 컬럼 ──────────────────────────────
        builder.Property(x => x.Embedding)
            .HasColumnType("vector(1024)")
            .HasConversion(vectorConverter);

        // ── 인덱스 ───────────────────────────────────
        builder.HasIndex(x => x.SearchVector)
            .HasMethod("GIN")
            .HasDatabaseName("ix_audit_logs_search_vector");

        builder.HasIndex(x => new { x.SearchVector, x.CreatedAt })
            .HasMethod("GIN")
            .HasDatabaseName("ix_audit_logs_vector_date");

        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("ix_audit_logs_user_id");

        builder.HasIndex(x => x.ActionType)
            .HasDatabaseName("ix_audit_logs_action_type");

        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("ix_audit_logs_created_at");

        builder.HasIndex(x => x.IsSuccess)
            .HasDatabaseName("ix_audit_logs_is_success")
            .HasFilter("is_success = false");

        // ── Vector HNSW 인덱스 ───────────────────────
        // 코사인 유사도 검색용!
        builder.HasIndex(x => x.Embedding)
            .HasMethod("hnsw")
            .HasDatabaseName("ix_audit_logs_embedding")
            .HasOperators("vector_cosine_ops");
    }
}
