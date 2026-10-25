using Buddham.Core.Entities.Auth;
using Buddham.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Buddham.Shared.Configurations.Auth;

public class ApplicationRoleConfiguration
    : IEntityTypeConfiguration<ApplicationRole>, IAuthConfiguration
{
    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {
        builder.ToTable(TableNames.Roles, SchemaTypes.Auth);

        builder.Property(x => x.Id).HasColumnName(ColumnNames.Id).HasColumnType(PgTypes.Uuid)
            .HasValueGenerator<SequentialGuidValueGenerator>();

        builder.Property(x => x.KoName).HasMaxLength(256).HasColumnType(PgTypes.Text).IsRequired(false);
        builder.Property(x => x.Description).HasColumnType(PgTypes.Text).IsRequired(false);
        builder.Property(x => x.SortOrder).HasDefaultValue(0);
        builder.Property(x => x.IsSystem).HasDefaultValue(false);
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).IsRequired().HasDefaultValueSql("NOW()");
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);

        // ── 운영 인덱스 ───────────────────────────────

        builder.HasIndex(x => x.IsActive).HasDatabaseName("ix_roles_is_active").HasFilter("is_active = true");
        builder.HasIndex(x => x.IsSystem).HasDatabaseName("ix_roles_is_system").HasFilter("is_system = true");
        builder.HasIndex(x => x.SortOrder).HasDatabaseName("ix_roles_sort_order");
        builder.HasIndex(x => x.DeletedAt).HasDatabaseName("ix_roles_deleted_at").HasFilter("deleted_at IS NULL");

        // trgm 검색
        builder.HasIndex(x => x.Name).HasMethod("GIN").HasDatabaseName("ix_roles_name").HasOperators("gin_trgm_ops");

        builder.HasIndex(x => x.Description).HasMethod("GIN").HasDatabaseName("ix_roles_description")
            .HasOperators("gin_trgm_ops");
    }
}
