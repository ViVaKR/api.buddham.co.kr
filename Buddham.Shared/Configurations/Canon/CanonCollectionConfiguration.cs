using Buddham.Core.Entities.Canon;
using Buddham.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buddham.Shared.Configurations.Canon;

public class CanonCollectionConfiguration : IEntityTypeConfiguration<CanonCollection>, ICanonConfiguration
{
    public void Configure(EntityTypeBuilder<CanonCollection> builder)
    {
        builder.ToTable("canon_collections", SchemaTypes.Canon);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName(ColumnNames.Id).HasColumnType(PgTypes.BigInt).ValueGeneratedOnAdd();

        builder.Property(x => x.NameKo).HasColumnName("name_ko").HasColumnType(PgTypes.Text).IsRequired();
        builder.Property(x => x.NameOriginal).HasColumnName("name_original").HasColumnType(PgTypes.Text).IsRequired(false);
        builder.Property(x => x.Abbreviation).HasColumnName("abbreviation").HasColumnType(PgTypes.Text).IsRequired(false);
        builder.Property(x => x.Description).HasColumnName("description").HasColumnType(PgTypes.Text).IsRequired(false);
        builder.Property(x => x.Tradition).HasColumnName("tradition").HasColumnType(PgTypes.Text).IsRequired(false);
        builder.Property(x => x.PublishedYear).HasColumnName("published_year").IsRequired(false);
        builder.Property(x => x.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);

        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").IsRequired(false);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired(false);

        // 약호(K, T, N, P)는 총서마다 고유해야 함 — NULL은 여러 개 허용
        builder.HasIndex(x => x.Abbreviation)
            .IsUnique()
            .HasFilter("abbreviation IS NOT NULL")
            .HasDatabaseName("ix_canon_collections_abbreviation");

        builder.HasIndex(x => x.SortOrder).HasDatabaseName("ix_canon_collections_sort_order");
    }
}
