using Buddham.Core.Entities.Scriptures;
using Buddham.Shared.Extensions.Scripture;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buddham.Shared.Configurations.Scripture;

public class CanonWorkIdentifierConfiguration : ScriptureConfigurationBase<CanonWorkIdentifier>
{
    protected override string TableName => "canon_work_identifiers";

    protected override void ConfigureEntity(EntityTypeBuilder<CanonWorkIdentifier> builder)
    {
        builder.Property(x => x.Scheme).IsRequired().HasMaxLength(32);
        builder.Property(x => x.Value).IsRequired().HasMaxLength(100);

        builder.HasActiveUniqueIndex(x => new { x.Scheme, x.Value }, "ux_canon_work_identifiers_scheme_value");

        // 작품당 대표 식별자는 하나만
        builder.HasIndex(x => x.WorkId, "ux_canon_work_identifiers_one_primary")
            .IsUnique()
            .HasFilter("is_primary AND deleted_at IS NULL");

        builder.HasOne(x => x.Work).WithMany(x => x.Identifiers).HasForeignKey(x => x.WorkId);
    }
}
