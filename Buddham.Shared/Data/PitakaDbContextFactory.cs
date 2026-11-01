using Buddham.Shared.Constants;
using Buddham.Shared.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Buddham.Shared.Data;

public class PitakaDbContextFactory : IDesignTimeDbContextFactory<PitakaDbContext>
{
    public PitakaDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PitakaDbContext>()
            .UseBuddhamMigration(SchemaTypes.Pitaka).Options;

        return new PitakaDbContext(options);

    }
}
