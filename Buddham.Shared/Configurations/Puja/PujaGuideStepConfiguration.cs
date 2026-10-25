using Buddham.Core.Entities.Puja;
using Buddham.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buddham.Shared.Configurations.Puja;

public class PujaGuideStepConfiguration : IEntityTypeConfiguration<PujaGuideStep>, IPujaConfiguration
{
    public void Configure(EntityTypeBuilder<PujaGuideStep> builder)
    {
        builder.ToTable("puja_guide_steps", SchemaTypes.Puja);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName(ColumnNames.Id).HasColumnType(PgTypes.BigInt).ValueGeneratedOnAdd();

        builder.Property(x => x.GuideId).HasColumnName("guide_id").IsRequired();
        builder.Property(x => x.StepNumber).HasColumnName("step_number").IsRequired();
        builder.Property(x => x.Title).HasColumnName("title").HasColumnType(PgTypes.Text).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasColumnType(PgTypes.Text).IsRequired();

        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").IsRequired(false);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired(false);

        // 같은 스키마 내부 관계 — 가이드 삭제 시 단계도 함께 정리 (Cascade)
        builder.HasOne(x => x.Guide)
            .WithMany(x => x.Steps)
            .HasForeignKey(x => x.GuideId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.GuideId, x.StepNumber })
            .IsUnique()
            .HasDatabaseName("ix_puja_guide_steps_guide_id_step_number");
    }
}
