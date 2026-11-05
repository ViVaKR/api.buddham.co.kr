using Buddham.Core.Entities.Scriptures;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Buddham.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Buddham.Shared.Extensions.Scripture;

namespace Buddham.Shared.Configurations.Scripture;

public class CanonSpeakerConfiguration : ScriptureConfigurationBase<CanonSpeaker>
{
    protected override string TableName => "canon_speakers";

    protected override void ConfigureEntity(EntityTypeBuilder<CanonSpeaker> builder)
    {
        builder.ConfigureCodedName();
        builder.Property(x => x.Description).HasColumnType(PgTypes.Text);

        builder.HasActiveUniqueIndex(x => x.Code, "ux_canon_speakers_code");
    }
}
