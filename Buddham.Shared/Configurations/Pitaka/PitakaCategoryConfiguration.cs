using Buddham.Core.Entities.Pitaka;
using Buddham.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buddham.Shared.Configurations.Pitaka;

public class PitakaCategoryConfiguration : IEntityTypeConfiguration<PitakaCategory>, IPitakaConfiguration
{
    public void Configure(EntityTypeBuilder<PitakaCategory> builder)
    {
        builder.ToTable("pitaka_categories", SchemaTypes.Pitaka);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName(ColumnNames.Id).HasColumnType(PgTypes.BigInt).ValueGeneratedOnAdd();

        builder.Property(x => x.NameKo).HasColumnName("name_ko").HasColumnType(PgTypes.Text).IsRequired();
        builder.Property(x => x.NamePali).HasColumnName("name_pali").HasColumnType(PgTypes.Text).IsRequired(false);
        builder.Property(x => x.Description).HasColumnName("description").HasColumnType(PgTypes.Text).IsRequired(false);
        builder.Property(x => x.ParentId).HasColumnName("parent_id").IsRequired(false);
        builder.Property(x => x.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);

        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").IsRequired(false);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired(false);

        // 자기참조 (Restrict: 자식 있는 카테고리를 실수로 지우면 막음)
        builder.HasOne(x => x.Parent)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ParentId, x.SortOrder })
            .HasDatabaseName("ix_pitaka_categories_parent_id_sort_order");
    }
}
