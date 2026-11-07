namespace Buddham.Shared.Constants;

public static class AppInfrastructure
{
    public const string ProtectionName = "Buddham.App";
    public const string MigrationsAssembly = "Buddham.Migrations";
    public const string DefaultConnection = "DefaultConnection";
    public const string MigrationConnection = "MigrationConnection";
    public const string RedisConnection = "RedisConnection";
    public const string MigrationHistoryTable = "ef_migrations_history";
    public const string CorsPolicy = "CorsPolicy";
    public const string SecurityStamp = "security_stamp";
    public const string TrustedDeviceKey = "trusted_device_key";
    public const string TrustedDeviceExpiry = "trusted_device_expiry";

    public const int EmbeddingDimensions = 1024;
    public const int EmbeddingContentChars = 500;
    public const int MaxTitleLength = 500;
    public const int MaxContentLength = 100_000;
    public const int MaxKeywordLength = 200;
    public const int MaxPageSize = 100;
    public const int MaxOffsetDepth = 10_000;

    // 벡터 검색은 Offset 에뮬레이션이라 깊은 페이지일수록 비싸다. 상한을 둔다.
    public const int MaxSemanticOffset = 1_000;

    public const string CacheKey_Embed = "embed";
}
