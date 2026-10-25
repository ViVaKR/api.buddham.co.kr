using Buddham.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Buddham.Shared.Data;

public class BuddhamDbContextFactory : IDesignTimeDbContextFactory<BuddhamDbContext>
{
    public BuddhamDbContext CreateDbContext(string[] args)
    {
        // appsettings.json 읽기
        var config = new ConfigurationBuilder()
            // 시작 프로젝트 (-s Project)
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            // 개발환경 오버라이드
            .AddJsonFile("appsettings.Development.json", optional: true)
            // DbContext 가 있는곳
            .AddUserSecrets(typeof(BuddhamDbContext).Assembly)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = config.GetConnectionString(AppInfrastructure.DefaultConnection)
                               ?? throw new InvalidOperationException($"{AppInfrastructure.DefaultConnection} 이 없습니다!");

        var optionsBuilder = new DbContextOptionsBuilder<BuddhamDbContext>();

        optionsBuilder.UseNpgsql(connectionString, npg =>
            {
                npg.MigrationsAssembly(AppInfrastructure.MigrationsAssembly);
                // 공간 데이터
                npg.UseNetTopologySuite();
                // AI 임베딩 벡터
                npg.UseVector();
                // 마이그레이션테이블, 스키마 
                npg.MigrationsHistoryTable(AppInfrastructure.MigrationHistoryTable, SchemaTypes.Auth);
            })
            .UseSnakeCaseNamingConvention();

        return new BuddhamDbContext(optionsBuilder.Options);
    }
}