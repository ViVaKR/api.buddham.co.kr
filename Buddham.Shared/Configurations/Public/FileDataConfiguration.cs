using Buddham.Core.Entities.Public;
using Buddham.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buddham.Shared.Configurations.Public;

public class FileDataConfiguration : IEntityTypeConfiguration<FileData>, IPublicConfiguration
{
    public void Configure(EntityTypeBuilder<FileData> builder)
    {
        builder.ToTable(TableNames.FileDatas, SchemaTypes.Public);

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName(ColumnNames.Id).HasColumnType(PgTypes.BigInt).ValueGeneratedOnAdd();

        builder.Property(x => x.UserId).IsRequired().HasColumnName("user_id").HasColumnType(PgTypes.Uuid);
        builder.Property(x => x.StoredFileName).IsRequired().HasColumnName("stored_file_name");
        builder.Property(x => x.Path).HasColumnName("path").HasColumnType(PgTypes.Text);
        builder.Property(x => x.ThumbnailPath).HasColumnName("thumbnail_path");
        builder.Property(x => x.WebpPath).HasColumnName("webp_path").HasColumnType(PgTypes.Text);
        builder.Property(x => x.Size).HasColumnType(PgTypes.BigInt).HasColumnName("size");
        builder.Property(x => x.OriginalFilename).IsRequired().HasColumnName("original_filename");
        builder.Property(x => x.IsShared).HasDefaultValue(false).HasColumnName("is_shared");
        builder.Property(x => x.FileType).HasColumnName("file_type");
        builder.Property(x => x.Status).HasColumnName("status");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired(false);
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").IsRequired(false);

        builder.HasIndex(x => x.StoredFileName).IsUnique().HasDatabaseName("ix_file_datas_stored_file_name");
        builder.HasIndex(x => new { x.UserId, x.CreatedAt }).HasDatabaseName("ix_file_datas_user_id_created_at");
        builder.HasIndex(x => x.OriginalFilename).HasDatabaseName("ix_file_datas_original_filename");
        builder.HasIndex(x => x.CreatedAt).HasDatabaseName("ix_file_datas_created_at");
    }
}