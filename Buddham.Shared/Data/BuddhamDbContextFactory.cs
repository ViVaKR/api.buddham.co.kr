using Buddham.Shared.Constants;
using Buddham.Shared.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Buddham.Shared.Data;

public class BuddhamDbContextFactory : IDesignTimeDbContextFactory<BuddhamDbContext>
{
    public BuddhamDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<BuddhamDbContext>()
            .UseBuddhamMigration(SchemaTypes.Auth).Options;
        return new BuddhamDbContext(options);
    }
}
