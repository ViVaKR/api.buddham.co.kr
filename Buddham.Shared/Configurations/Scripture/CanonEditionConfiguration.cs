using Buddham.Core.Entities.Scriptures;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buddham.Shared.Configurations.Scripture;

public class CanonEditionConfiguration : ScriptureConfigurationBase<CanonEdition>
{
    protected override string TableName => "canon_editions";

    protected override void ConfigureEntity(EntityTypeBuilder<CanonEdition> builder)
    {
        builder.Property(x => x.LanguageCode).IsRequired().HasMaxLength(16);
        builder.Property(x => x.Script).HasMaxLength(16);
        builder.Property(x => x.TranslatorName).HasMaxLength(300);
        builder.Property(x => x.Revision).HasMaxLength(100);
        builder.Property(x => x.ContentHash).HasMaxLength(64);

        builder.HasIndex(x => new { x.WorkId, x.LanguageCode, x.Kind });

        builder.HasOne(x => x.Work).WithMany(x => x.Editions).HasForeignKey(x => x.WorkId);
        builder.HasOne(x => x.Source).WithMany(x => x.Editions).HasForeignKey(x => x.SourceId);

        // 번역 → 저본 원문 판본
        builder.HasOne(x => x.BasedOn).WithMany(x => x.Translations).HasForeignKey(x => x.BasedOnEditionId);
    }
}
