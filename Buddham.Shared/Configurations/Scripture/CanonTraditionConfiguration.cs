using Buddham.Core.Entities.Scriptures;
using Buddham.Shared.Constants;
using Buddham.Shared.Extensions.Scripture;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buddham.Shared.Configurations.Scripture;

public class CanonTraditionConfiguration : ScriptureConfigurationBase<CanonTradition>
{
    protected override string TableName => "canon_traditions";

    protected override void ConfigureEntity(EntityTypeBuilder<CanonTradition> builder)
    {
        builder.ConfigureCodedName();
        builder.Property(x => x.Description).HasColumnType(PgTypes.Text);

        builder.HasActiveUniqueIndex(x => x.Code, "ux_canon_traditions_code");

        builder.HasOne(x => x.Parent).WithMany(x => x.Children).HasForeignKey(x => x.ParentId);
    }
}
