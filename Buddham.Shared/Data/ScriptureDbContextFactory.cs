using Buddham.Shared.Constants;
using Buddham.Shared.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Buddham.Shared.Data;

public class ScriptureDbContextFactory : IDesignTimeDbContextFactory<ScriptureDbContext>
{
    public ScriptureDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ScriptureDbContext>()
            .UseBuddhamMigration(SchemaTypes.Scripture).Options;

        return new ScriptureDbContext(options);
    }
}
