using Buddham.Shared.Constants;
using Buddham.Shared.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Buddham.Shared.Data;

public class PublicDbContextFactory : IDesignTimeDbContextFactory<PublicDbContext>
{
    public PublicDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PublicDbContext>()
               .UseBuddhamMigration(SchemaTypes.Public).Options;

        return new PublicDbContext(options);
    }
}
