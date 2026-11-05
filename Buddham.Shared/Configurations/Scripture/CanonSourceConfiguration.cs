using Buddham.Core.Entities.Scriptures;
using Buddham.Shared.Constants;
using Buddham.Shared.Extensions.Scripture;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buddham.Shared.Configurations.Scripture;

public class CanonSourceConfiguration : ScriptureConfigurationBase<CanonSource>
{
    protected override string TableName => "canon_sources";

    protected override void ConfigureEntity(EntityTypeBuilder<CanonSource> builder)
    {
        builder.Property(x => x.Code).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(300);
        builder.Property(x => x.Publisher).HasMaxLength(300);
        builder.Property(x => x.SourceUrl).HasMaxLength(1000);
        builder.Property(x => x.LicenseName).HasMaxLength(200);
        builder.Property(x => x.LicenseUrl).HasMaxLength(1000);
        builder.Property(x => x.Version).HasMaxLength(100);
        builder.Property(x => x.Note).HasColumnType(PgTypes.Text);

        builder.HasActiveUniqueIndex(x => x.Code, "ux_canon_sources_code");
    }
}
