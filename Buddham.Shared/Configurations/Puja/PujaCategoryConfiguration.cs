using Buddham.Core.Entities.Puja;
using Buddham.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buddham.Shared.Configurations.Puja;

public class PujaCategoryConfiguration : IEntityTypeConfiguration<PujaCategory>, IPujaConfiguration
{
    public void Configure(EntityTypeBuilder<PujaCategory> builder)
    {
        builder.ToTable("puja_categories", SchemaTypes.Puja);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName(ColumnNames.Id).HasColumnType(PgTypes.BigInt).ValueGeneratedOnAdd();

        builder.Property(x => x.NameKo).HasColumnName("name_ko").HasColumnType(PgTypes.Text).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasColumnType(PgTypes.Text).IsRequired(false);
        builder.Property(x => x.ParentId).HasColumnName("parent_id").IsRequired(false);
        builder.Property(x => x.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);

        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").IsRequired(false);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired(false);

        builder.HasOne(x => x.Parent)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ParentId, x.SortOrder })
            .HasDatabaseName("ix_puja_categories_parent_id_sort_order");
    }
}
