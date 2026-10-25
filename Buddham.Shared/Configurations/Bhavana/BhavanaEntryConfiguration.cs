using Buddham.Core.Entities.Bhavana;
using Buddham.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buddham.Shared.Configurations.Bhavana;

public class BhavanaEntryConfiguration : IEntityTypeConfiguration<BhavanaEntry>, IBhavanaConfiguration
{
    public void Configure(EntityTypeBuilder<BhavanaEntry> builder)
    {
        builder.ToTable("bhavana_entries", SchemaTypes.Bhavana);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName(ColumnNames.Id).HasColumnType(PgTypes.BigInt).ValueGeneratedOnAdd();

        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.AuthorPseudonym).HasColumnName("author_pseudonym").HasColumnType(PgTypes.Text).IsRequired(false);

        builder.Property(x => x.PitakaScriptureId).HasColumnName("pitaka_scripture_id").IsRequired(false);

        builder.Property(x => x.SourceTitle).HasColumnName("source_title").HasColumnType(PgTypes.Text).IsRequired();
        builder.Property(x => x.SourceContent).HasColumnName("source_content").HasColumnType(PgTypes.Text).IsRequired();

        builder.Property(x => x.TypedContent).HasColumnName("typed_content").HasColumnType(PgTypes.Text).IsRequired();

        builder.Property(x => x.TotalLength).HasColumnName("total_length").HasDefaultValue(0);
        builder.Property(x => x.TypedLength).HasColumnName("typed_length").HasDefaultValue(0);

        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasColumnType(PgTypes.Text).IsRequired();
        builder.Property(x => x.IsPublic).HasColumnName("is_public").HasDefaultValue(false);

        builder.Property(x => x.StartedAt).HasColumnName("started_at").IsRequired();
        builder.Property(x => x.LastPracticedAt).HasColumnName("last_practiced_at").IsRequired(false);
        builder.Property(x => x.CompletedAt).HasColumnName("completed_at").IsRequired(false);

        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").IsRequired(false);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired(false);

        // 내 진행중/완료 목록 조회용
        builder.HasIndex(x => new { x.UserId, x.Status })
            .HasDatabaseName("ix_bhavana_entries_user_id_status");

        // 특정 원문을 사경한 기록들 조회용 (스칼라 참조라 FK는 없지만 인덱스는 걸어둠)
        builder.HasIndex(x => x.PitakaScriptureId)
            .HasDatabaseName("ix_bhavana_entries_pitaka_scripture_id");

        // 상가 피드 조회용 (공개된 완료작만 최신순으로 뽑을 때)
        builder.HasIndex(x => new { x.IsPublic, x.Status, x.CompletedAt })
            .HasDatabaseName("ix_bhavana_entries_is_public_status_completed_at");
    }
}