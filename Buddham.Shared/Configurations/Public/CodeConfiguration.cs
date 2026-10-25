using Buddham.Core.Entities.Code;
using Buddham.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buddham.Shared.Configurations.Public;

public class CodeConfiguration : IEntityTypeConfiguration<Code>
{
  public void Configure(EntityTypeBuilder<Code> builder)
  {

    builder.ToTable(TableNames.Codes, SchemaTypes.Public);

    builder.HasKey(x => x.Id);

    builder.Property(x => x.Id).HasColumnName(ColumnNames.Id)
      .HasColumnType(PgTypes.BigInt).ValueGeneratedOnAdd();

    builder.Property(x => x.Title).HasColumnName("title")
      .HasColumnType(PgTypes.Text);

    builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").IsRequired(false);
    builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
    builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired(false);
  }
}
