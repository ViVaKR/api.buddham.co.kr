using Buddham.Core.Common.Options;

namespace Buddham.Core.Interfaces.Cache;

public interface IEntityCacheFactory
{
    IEntityCache Create(EntityCacheOptions options);
}
