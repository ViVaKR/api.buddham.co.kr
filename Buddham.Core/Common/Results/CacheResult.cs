namespace Buddham.Core.Common.Results;

public record CacheResult<T>(T? Data, DateTime? ExpirationTime, bool IsCached);
