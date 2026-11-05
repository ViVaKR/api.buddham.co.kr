using Buddham.Core.Entities.Scriptures;
using Buddham.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buddham.Shared.Configurations.Scripture;

public class CanonParallelConfiguration : ScriptureConfigurationBase<CanonParallel>
{
    protected override string TableName => "canon_parallels";

    protected override void ConfigureEntity(EntityTypeBuilder<CanonParallel> builder)
    {
        builder.Property(x => x.Note).HasColumnType(PgTypes.Text);

        builder.ToTable(t => t.HasCheckConstraint(
            "ck_canon_parallels_distinct_works", "from_work_id <> to_work_id"));

        // 같은 엔터티를 두 번 참조하므로 양쪽 모두 명시
        builder.HasOne(x => x.FromWork).WithMany(x => x.ParallelsFrom).HasForeignKey(x => x.FromWorkId);
        builder.HasOne(x => x.ToWork).WithMany(x => x.ParallelsTo).HasForeignKey(x => x.ToWorkId);
        builder.HasOne(x => x.FromSegment).WithMany().HasForeignKey(x => x.FromSegmentId);
        builder.HasOne(x => x.ToSegment).WithMany().HasForeignKey(x => x.ToSegmentId);
    }
}
