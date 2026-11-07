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
    public const int MaxKeywordLength = 200;
    public const int MaxPageSize = 100;
}
