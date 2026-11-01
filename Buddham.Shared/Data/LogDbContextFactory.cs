using Buddham.Shared.Constants;
using Buddham.Shared.Extensions;
using EFCore.NamingConventions.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Buddham.Shared.Data;

public class LogDbContextFactory : IDesignTimeDbContextFactory<LogDbContext>
{
    public LogDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<LogDbContext>()
            .UseBuddhamMigration(SchemaTypes.Log).Options;

        return new LogDbContext(options);
    }
}
