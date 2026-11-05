using Buddham.Core.Common.Primitives;
using Buddham.Shared.Constants;
using Buddham.Shared.Extensions.Scripture;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buddham.Shared.Configurations.Scripture;

public abstract class ScriptureConfigurationBase<T> : IEntityTypeConfiguration<T>, IScriptureConfiguration
    where T : EntityBase
{
    protected abstract string TableName { get; }

    public void Configure(EntityTypeBuilder<T> builder)
    {
        builder.ConfigureEntityBase(TableName, SchemaTypes.Scripture);
        ConfigureEntity(builder);
    }

    protected abstract void ConfigureEntity(EntityTypeBuilder<T> builder);
}
