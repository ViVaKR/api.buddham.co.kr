using Buddham.Core.Entities.Public;
using Buddham.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buddham.Shared.Configurations.Public;

public class AppleSyscallConfiguratio : IEntityTypeConfiguration<AppleSyscall>, IPublicConfiguration
{
    public void Configure(EntityTypeBuilder<AppleSyscall> builder)
    {
        builder.ToTable(TableNames.AppleSyscalls, SchemaTypes.Public);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName(ColumnNames.Id).HasColumnType(PgTypes.BigInt).ValueGeneratedOnAdd();
        builder.Property(x => x.SyscallNumber).HasColumnName("syscall_number");
        builder.Property(x => x.FuncName).HasColumnName("func_name").HasColumnType(PgTypes.Text);
        builder.Property(x => x.ReturnType).HasColumnName("return_type").HasColumnType(PgTypes.Text);
        builder.Property(x => x.Arguments).HasColumnName("arguments").HasColumnType(PgTypes.Text);
        builder.Property(x => x.ExampleAsm).HasColumnName("example_asm").HasColumnType(PgTypes.Text).IsRequired(false);
        builder.Property(x => x.ExampleC).HasColumnName("example_c").HasColumnType(PgTypes.Text).IsRequired(false);
        builder.Property(x => x.Description).HasColumnName("description").HasColumnType(PgTypes.Text).IsRequired(false);

        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").IsRequired(false);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired(false);

        builder.HasIndex(x => x.SyscallNumber).HasDatabaseName("ix_syscall_number").IsUnique();
        builder.HasIndex(x => x.FuncName).HasDatabaseName("ix_func_name");
    }
}
