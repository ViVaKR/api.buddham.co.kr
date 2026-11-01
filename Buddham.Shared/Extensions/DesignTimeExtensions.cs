using Buddham.Shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace Buddham.Shared.Extensions;

internal static class DesignTimeExtensions
{
    public static DbContextOptionsBuilder<T> UseBuddhamMigration<T>(
            this DbContextOptionsBuilder<T> builder, string historySchema)
            where T : DbContext
    {
        var cs = DesignTimeHelper.GetMigrationConnectionString<T>();

        builder.UseNpgsql(cs, npg =>
        {
            npg.MigrationsAssembly(AppInfrastructure.MigrationsAssembly);
            npg.UseNetTopologySuite();
            npg.UseVector();
            npg.MigrationsHistoryTable(AppInfrastructure.MigrationHistoryTable, historySchema);
        }).UseSnakeCaseNamingConvention();

        return builder;
    }
}
