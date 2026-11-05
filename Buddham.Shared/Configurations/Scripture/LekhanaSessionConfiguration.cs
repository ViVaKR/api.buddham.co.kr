using Buddham.Core.Entities.Scriptures;
using Buddham.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buddham.Shared.Configurations.Scripture;

public class LekhanaSessionConfiguration : ScriptureConfigurationBase<LekhanaSession>
{
    protected override string TableName => "lekhana_sessions";
    protected override void ConfigureEntity(EntityTypeBuilder<LekhanaSession> builder)
    {
        builder.Property(x => x.UserId).IsRequired().HasColumnType(PgTypes.Uuid);
        builder.Property(x => x.Title).HasMaxLength(300);
        builder.Property(x => x.Vow).HasColumnType(PgTypes.Text);

        builder.HasIndex(x => new { x.UserId, x.Status });
        builder.HasOne(x => x.Edition).WithMany().HasForeignKey(x => x.EditionId);
    }
}
