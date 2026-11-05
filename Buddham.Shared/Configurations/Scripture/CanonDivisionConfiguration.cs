using Buddham.Core.Entities.Scriptures;
using Buddham.Shared.Constants;
using Buddham.Shared.Extensions.Scripture;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buddham.Shared.Configurations.Scripture;

public class CanonDivisionConfiguration : ScriptureConfigurationBase<CanonDivision>
{
    protected override string TableName => "canon_divisions";

    protected override void ConfigureEntity(EntityTypeBuilder<CanonDivision> builder)
    {
        builder.ConfigureCodedName();

        // Code 는 같은 Canon 안에서 유일 (부모가 달라도 중복 불가) → 경로형 키
        builder.HasActiveUniqueIndex(x => new { x.CanonId, x.Code }, "ux_canon_divisions_canon_code");

        builder.HasOne(x => x.Canon).WithMany(x => x.Divisions).HasForeignKey(x => x.CanonId);
        builder.HasOne(x => x.Parent).WithMany(x => x.Children).HasForeignKey(x => x.ParentId);
    }
}
