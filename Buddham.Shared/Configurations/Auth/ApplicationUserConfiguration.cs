using Buddham.Core.Entities.Auth;
using Buddham.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Buddham.Shared.Configurations.Auth;

public class ApplicationUserConfiguration
    : IEntityTypeConfiguration<ApplicationUser>, IAuthConfiguration
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable(TableNames.Users, SchemaTypes.Auth);

        builder.Ignore(x => x.PhoneNumber);
        builder.Ignore(x => x.PhoneNumberConfirmed);

        builder.Property(x => x.Id)
            .HasColumnName(ColumnNames.Id)
            .HasColumnType(PgTypes.Uuid)
            .HasValueGenerator<SequentialGuidValueGenerator>();

        builder.Property(x => x.FullName)
            .IsRequired()
            .HasMaxLength(256)
            .HasColumnType(PgTypes.Text);

        builder.Property(x => x.Pseudonym).IsRequired().HasMaxLength(50).HasColumnType(PgTypes.Text);
        builder.Property(x => x.Avatar).HasMaxLength(256).HasColumnType(PgTypes.Text);
        builder.Property(x => x.RefreshToken).HasColumnType(PgTypes.Text);
        builder.Property(x => x.RefreshTokenExpiryTime).HasColumnType(PgTypes.TimestampTz);
        builder.Property(x => x.TrustedDeviceKey).HasColumnType(PgTypes.Text);
        builder.Property(x => x.TrustedDeviceExpiry).HasColumnType(PgTypes.TimestampTz);
        builder.Property(x => x.Introduction).HasColumnType(PgTypes.Text).IsRequired(false);
        builder.Property(x => x.TimeZone).HasMaxLength(50).HasColumnType(PgTypes.Text);
        builder.Property(x => x.Locale).HasMaxLength(10).HasColumnType(PgTypes.Text);
        builder.Property(x => x.Provider).HasMaxLength(20).HasColumnType(PgTypes.Text).HasDefaultValue("buddham.co.kr");
        builder.Property(x => x.LastLoginAt).IsRequired(false);
        builder.Property(x => x.LastLoginIp).HasMaxLength(45).HasColumnType(PgTypes.Text).IsRequired(false);
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.CreatedAt).IsRequired().HasDefaultValueSql("NOW()");
        builder.Property(x => x.UpdatedAt).IsRequired(false);

        // ── 인덱스 ──────────────────────────
        builder.HasIndex(x => x.Pseudonym).HasDatabaseName("ix_users_pseudonym");

        // 소프트 삭제 필터
        builder.HasIndex(x => x.DeletedAt).HasDatabaseName("ix_users_deleted_at").HasFilter("deleted_at IS NULL");

        // 활성 사용자 필터 (대부분의 조회 대상)
        builder.HasIndex(x => x.IsActive).HasDatabaseName("ix_users_is_active").HasFilter("is_active = true");

        // 가입 경로별 통계/조회
        builder.HasIndex(x => x.Provider).HasDatabaseName("ix_users_provider");

        // 보안 감사 - 마지막 로그인 시각
        builder.HasIndex(x => x.LastLoginAt).HasDatabaseName("ix_users_last_login_at");

        // 가입일 정렬
        // CreatedAt 내림차순, Id 내림차순을 한꺼번에 타는 광속 인덱스
        builder.HasIndex(x => new { x.CreatedAt, x.Id }).HasDatabaseName("ix_users_created_at_id_desc").IsDescending(true, true);

        // RefreshToken 조회 (토큰 갱신 시 빠른 조회)
        builder.HasIndex(x => x.RefreshToken).HasDatabaseName("ix_users_refresh_token").HasFilter("refresh_token IS NOT NULL");

        builder.HasIndex(x => x.FullName).HasMethod("GIN").HasDatabaseName("ix_users_full_name_trgm").HasOperators("gin_trgm_ops");

        // 필명 초고속 부분/모호 검색(LIKE/ILIKE) 전용 Gin 인덱스 벨트!
        builder.HasIndex(x => x.Pseudonym).HasDatabaseName("ix_users_pseudonym_trgm").HasMethod("GIN").HasOperators("gin_trgm_ops");

        builder.HasIndex(x => x.Introduction).HasDatabaseName("ix_users_introduction").HasMethod("GIN").HasOperators("gin_trgm_ops");
    }
}
