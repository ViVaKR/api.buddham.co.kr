using Buddham.Core.Interfaces.Cache;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Buddham.Shared.Infrastructure;

public class ServiceDependencies<TContext>(
    TContext context,
    ILoggerFactory loggerFactory,
    IRedisCacheService cacheService,
    IConfiguration configuration
)
{
    public TContext Context => context;
    public ILoggerFactory LoggerFactory => loggerFactory;
    public IRedisCacheService CacheService => cacheService;
    public IConfiguration Configuration => configuration;
}