using Buddham.Shared.Constants;
using Buddham.Shared.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Buddham.Shared.Data;

public class CanonDbContextFactory : IDesignTimeDbContextFactory<CanonDbContext>
{
    public CanonDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CanonDbContext>()
            .UseBuddhamMigration(SchemaTypes.Canon).Options;

        return new CanonDbContext(options);
    }
}
