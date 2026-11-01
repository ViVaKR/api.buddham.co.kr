using Buddham.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Buddham.Shared.Extensions;

internal static class DesignTimeHelper
{
    public static string GetMigrationConnectionString<TContext>() where TContext : DbContext
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets(typeof(TContext).Assembly)
            .AddEnvironmentVariables()
            .Build();

        return config.GetConnectionString(AppInfrastructure.MigrationConnection)
               ?? throw new InvalidOperationException(
                   $"{AppInfrastructure.MigrationConnection} 이 없습니다! user-secrets 를 확인하세요.");
    }
}
