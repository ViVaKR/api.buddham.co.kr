using Buddham.Core.Entities.Scriptures;
using Buddham.Shared.Constants;
using Buddham.Shared.Extensions.Scripture;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buddham.Shared.Configurations.Scripture;

public class CanonGroupConfiguration : ScriptureConfigurationBase<CanonGroup>
{
    protected override string TableName => "canon_groups";

    protected override void ConfigureEntity(EntityTypeBuilder<CanonGroup> builder)
    {
        builder.ConfigureCodedName();

        // Code 는 집성 안에서 유일 (예: sn.56). 루트(부모 NULL) 중복을 막기 위해 부모를 키에 넣지 않음
        builder.HasActiveUniqueIndex(x => new { x.CollectionId, x.Code }, "ux_canon_groups_collection_code");

        builder.HasOne(x => x.Collection).WithMany(x => x.Groups).HasForeignKey(x => x.CollectionId);
        builder.HasOne(x => x.Parent).WithMany(x => x.Children).HasForeignKey(x => x.ParentId);
    }
}
