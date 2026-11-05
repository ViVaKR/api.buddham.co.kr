using Buddham.Core.Entities.Scriptures;
using Buddham.Shared.Constants;
using Buddham.Shared.Extensions.Scripture;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buddham.Shared.Configurations.Scripture;

public class CanonWorkConfiguration : ScriptureConfigurationBase<CanonWork>
{
    protected override string TableName => "canon_works";

    protected override void ConfigureEntity(EntityTypeBuilder<CanonWork> builder)
    {
        builder.Property(x => x.Code).IsRequired().HasMaxLength(100);
        builder.Property(x => x.TitleKo).IsRequired().HasMaxLength(500);
        builder.Property(x => x.TitleOriginal).HasMaxLength(1000);
        builder.Property(x => x.TitleRoman).HasMaxLength(500);
        builder.Property(x => x.Summary).HasColumnType(PgTypes.Text);
        builder.Property(x => x.AttributionNote).HasColumnType(PgTypes.Text);

        builder.HasActiveUniqueIndex(x => new { x.CollectionId, x.Code }, "ux_canon_works_collection_code");
        builder.HasIndex(x => new { x.GroupId, x.SortOrder });

        builder.HasOne(x => x.Collection).WithMany(x => x.Works).HasForeignKey(x => x.CollectionId);
        builder.HasOne(x => x.Group).WithMany(x => x.Works).HasForeignKey(x => x.GroupId);
        builder.HasOne(x => x.Speaker).WithMany(x => x.Works).HasForeignKey(x => x.SpeakerId);

        // 주석서 → 원전 (FK 이름이 관례와 달라 명시 필요)
        builder.HasOne(x => x.CommentaryOf).WithMany(x => x.Commentaries).HasForeignKey(x => x.CommentaryOfWorkId);
    }
}
