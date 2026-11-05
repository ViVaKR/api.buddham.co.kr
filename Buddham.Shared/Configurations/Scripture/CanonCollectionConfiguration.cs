using Buddham.Core.Entities.Scriptures;
using Buddham.Shared.Constants;
using Buddham.Shared.Extensions.Scripture;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buddham.Shared.Configurations.Scripture;

public class CanonCollectionConfiguration : ScriptureConfigurationBase<CanonCollection>
{
    protected override string TableName => "canon_collections";

    protected override void ConfigureEntity(EntityTypeBuilder<CanonCollection> builder)
    {
        builder.ConfigureCodedName();
        builder.Property(x => x.TraditionNote).HasColumnType(PgTypes.Text);

        builder.HasActiveUniqueIndex(x => new { x.DivisionId, x.Code }, "ux_canon_collections_division_code");

        builder.HasOne(x => x.Division).WithMany(x => x.Collections).HasForeignKey(x => x.DivisionId);
        builder.HasOne(x => x.Tradition).WithMany(x => x.Collections).HasForeignKey(x => x.TraditionId);
    }
}
