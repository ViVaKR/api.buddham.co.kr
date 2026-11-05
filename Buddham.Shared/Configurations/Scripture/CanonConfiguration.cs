using Buddham.Core.Entities.Scriptures;
using Buddham.Shared.Constants;
using Buddham.Shared.Extensions.Scripture;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buddham.Shared.Configurations.Scripture;

public class CanonConfiguration : ScriptureConfigurationBase<Canon>
{
    protected override string TableName => "canons";

    protected override void ConfigureEntity(EntityTypeBuilder<Canon> builder)
    {
        builder.ConfigureCodedName();
        builder.Property(x => x.PrimaryLanguage).HasMaxLength(16);
        builder.Property(x => x.Description).HasColumnType(PgTypes.Text);

        builder.HasActiveUniqueIndex(x => x.Code, "ux_canons_code");

        builder.HasOne(x => x.Tradition).WithMany(x => x.Canons).HasForeignKey(x => x.TraditionId);
    }
}
