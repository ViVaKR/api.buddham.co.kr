using System.Reflection;
using Buddham.Core.Entities.Log;
using Buddham.Shared.Configurations;
using Buddham.Shared.Constants;
using Buddham.Shared.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Buddham.Shared.Data;

public class LogDbContext(DbContextOptions<LogDbContext> options) : DbContext(options)
{
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.RegisterModelCreating(SchemaTypes.Log);
        
        // 인터페이스를 활용한 클린 자동 스캔
        builder.ApplyConfigurationsFromAssembly(
            Assembly.GetExecutingAssembly(),
            type => typeof(ILogConfiguration).IsAssignableFrom(type) && type is { IsInterface: false, IsAbstract: false }
        );
    }
    
    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        // builder.Properties<Pgvector.Vector>().HaveColumnType(PgTypes.Vector); 
        builder.Properties<DateTime>().HaveColumnType(PgTypes.TimestampTz);
        builder.Properties<DateTime?>().HaveColumnType(PgTypes.TimestampTz);
    }
}
