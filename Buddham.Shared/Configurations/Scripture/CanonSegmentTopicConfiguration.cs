using Buddham.Core.Entities.Scriptures;
using Buddham.Shared.Constants;
using Buddham.Shared.Extensions.Scripture;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buddham.Shared.Configurations.Scripture;

public class CanonSegmentTopicConfiguration : ScriptureConfigurationBase<CanonSegmentTopic>
{
    protected override string TableName => "canon_segment_topics";

    protected override void ConfigureEntity(EntityTypeBuilder<CanonSegmentTopic> builder)
    {
        builder.Property(x => x.Note).HasColumnType(PgTypes.Text);
        builder.HasActiveUniqueIndex(x => new { x.SegmentId, x.TopicId },
        "ux_canon_segment_topics_segment_topic");

        builder.HasOne(x => x.Segment).WithMany(x => x.Topics).HasForeignKey(x => x.SegmentId);
        builder.HasOne(x => x.Topic).WithMany(x => x.Segments).HasForeignKey(x => x.TopicId);
    }
}
