using Buddham.Core.Entities.Scriptures;
using Buddham.Shared.Constants;
using Buddham.Shared.Extensions.Scripture;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buddham.Shared.Configurations.Scripture;

public class LekhanaEntryConfiguration : ScriptureConfigurationBase<LekhanaEntry>
{
    protected override string TableName => "lekhana_entries";

    protected override void ConfigureEntity(EntityTypeBuilder<LekhanaEntry> builder)
    {
        builder.Property(x => x.TypedText).HasColumnType(PgTypes.Text);
        builder.Property(x => x.ImageUrl).HasMaxLength(1000);

        builder.HasActiveUniqueIndex(x => new { x.SessionId, x.SegmentId },
        "ux_lekhana_entries_session_segment");
        builder.HasIndex(x => new { x.SessionId, x.SortOrder });

        builder.HasOne(x => x.Session).WithMany(x => x.Entries).HasForeignKey(x => x.SessionId);
        builder.HasOne(x => x.Segment).WithMany(x => x.LekhanaEntries).HasForeignKey(x => x.SegmentId);
    }
}
