using Buddham.Core.Entities.Canon;
using Buddham.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buddham.Shared.Configurations.Canon;

public class CanonWorkConfiguration : IEntityTypeConfiguration<CanonWork>, ICanonConfiguration
{
    public void Configure(EntityTypeBuilder<CanonWork> builder)
    {
        builder.ToTable("canon_works", SchemaTypes.Canon);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName(ColumnNames.Id).HasColumnType(PgTypes.BigInt).ValueGeneratedOnAdd();

        builder.Property(x => x.CollectionId).HasColumnName("collection_id").IsRequired();
        builder.Property(x => x.CanonicalNumber).HasColumnName("canonical_number").HasColumnType(PgTypes.Text).IsRequired(false);
        builder.Property(x => x.TitleKo).HasColumnName("title_ko").HasColumnType(PgTypes.Text).IsRequired();
        builder.Property(x => x.TitleOriginal).HasColumnName("title_original").HasColumnType(PgTypes.Text).IsRequired(false);
        builder.Property(x => x.TitleRomanized).HasColumnName("title_romanized").HasColumnType(PgTypes.Text).IsRequired(false);
        builder.Property(x => x.TranslatorOrAuthor).HasColumnName("translator_or_author").HasColumnType(PgTypes.Text).IsRequired(false);
        builder.Property(x => x.Era).HasColumnName("era").HasColumnType(PgTypes.Text).IsRequired(false);
        builder.Property(x => x.Description).HasColumnName("description").HasColumnType(PgTypes.Text).IsRequired(false);

        builder.Property(x => x.ParentId).HasColumnName("parent_id").IsRequired(false);
        builder.Property(x => x.VolumeNumber).HasColumnName("volume_number").IsRequired(false);
        builder.Property(x => x.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);

        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").IsRequired(false);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired(false);

        builder.HasOne(x => x.Collection)
            .WithMany(x => x.Works)
            .HasForeignKey(x => x.CollectionId)
            .OnDelete(DeleteBehavior.Restrict);

        // 자기참조 (다권본 - 부모 권이 있으면 자식 권 삭제 실수 방지)
        builder.HasOne(x => x.Parent)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        // 같은 총서 안에서 표준 번호는 유일해야 함
        builder.HasIndex(x => new { x.CollectionId, x.CanonicalNumber })
            .IsUnique()
            .HasFilter("canonical_number IS NOT NULL")
            .HasDatabaseName("ix_canon_works_collection_id_canonical_number");

        builder.HasIndex(x => new { x.ParentId, x.VolumeNumber })
            .HasDatabaseName("ix_canon_works_parent_id_volume_number");

        builder.HasIndex(x => x.CollectionId).HasDatabaseName("ix_canon_works_collection_id");
    }
}