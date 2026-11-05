using System.Reflection;
using Buddham.Core.Entities.Scriptures;
using Buddham.Shared.Configurations;
using Buddham.Shared.Constants;
using Buddham.Shared.Extensions;
using Buddham.Shared.Extensions.Scripture;
using Microsoft.EntityFrameworkCore;

namespace Buddham.Shared.Data;

public class ScriptureDbContext(DbContextOptions<ScriptureDbContext> options) : DbContext(options)
{
    // ── 경전 (Scriptures) ───────────────────────────────────────────

    // 계층
    public DbSet<CanonTradition> CanonTraditions => Set<CanonTradition>();
    public DbSet<Canon> Canons => Set<Canon>();
    public DbSet<CanonDivision> CanonDivisions => Set<CanonDivision>();
    public DbSet<CanonCollection> CanonCollections => Set<CanonCollection>();
    public DbSet<CanonGroup> CanonGroups => Set<CanonGroup>();

    // 작품•원본
    public DbSet<CanonSpeaker> CanonSpeakers => Set<CanonSpeaker>();
    public DbSet<CanonSource> CanonSources => Set<CanonSource>();
    public DbSet<CanonWork> CanonWorks => Set<CanonWork>();
    public DbSet<CanonWorkIdentifier> CanonWorkIdentifiers => Set<CanonWorkIdentifier>();
    public DbSet<CanonEdition> CanonEditions => Set<CanonEdition>();
    public DbSet<CanonSegment> CanonSegments => Set<CanonSegment>();
    public DbSet<CanonParallel> CanonParallels => Set<CanonParallel>();


    // 교리 주제 • 사경
    public DbSet<DhammaTopic> DhammaTopics => Set<DhammaTopic>();
    public DbSet<CanonSegmentTopic> CanonSegmentTopics => Set<CanonSegmentTopic>();
    public DbSet<LekhanaSession> LekhanaSessions => Set<LekhanaSession>();
    public DbSet<LekhanaEntry> LekhanaEntries => Set<LekhanaEntry>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.RegisterModelCreating(SchemaTypes.Scripture);

        // 경전 Configuration 만 적용 (IScriptureConfiguration 구현체)
        builder.ApplyConfigurationsFromAssembly(
            typeof(ScriptureDbContext).Assembly,
            type => type is { IsInterface: false, IsAbstract: false } &&
                    typeof(IScriptureConfiguration).IsAssignableFrom(type));

        // FK 는 모두 Restrict (반드시 Configuration 적용 뒤에 호출)
        builder.UseRestrictDeleteFor(typeof(CanonTradition).Namespace);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        builder.Properties<Pgvector.Vector>().HaveColumnType(PgTypes.Vector);
        builder.Properties<DateTime>().HaveColumnType(PgTypes.TimestampTz);
        builder.Properties<DateTime?>().HaveColumnType(PgTypes.TimestampTz);

        // 경전 열거형은 문자열로 저장
        builder.ConfigureScriptureConventions();
    }
}
