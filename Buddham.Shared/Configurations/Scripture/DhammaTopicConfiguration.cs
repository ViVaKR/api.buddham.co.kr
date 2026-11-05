using Buddham.Core.Entities.Scriptures;
using Buddham.Shared.Constants;
using Buddham.Shared.Extensions.Scripture;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buddham.Shared.Configurations.Scripture;

public class DhammaTopicConfiguration : ScriptureConfigurationBase<DhammaTopic>
{
    protected override string TableName => "dhamma_topics";

    protected override void ConfigureEntity(EntityTypeBuilder<DhammaTopic> builder)
    {
        builder.Property(x => x.Code).IsRequired().HasMaxLength(100);
        builder.Property(x => x.NameKo).IsRequired().HasMaxLength(300);
        builder.Property(x => x.NamePali).HasMaxLength(300);
        builder.Property(x => x.NameSanskrit).HasMaxLength(300);
        builder.Property(x => x.Description).HasColumnType(PgTypes.Text);

        builder.HasActiveUniqueIndex(x => x.Code, "ux_dhamma_topics_code");
        builder.HasOne(x => x.Parent).WithMany(x => x.Children).HasForeignKey(x => x.ParentId);
    }
}
