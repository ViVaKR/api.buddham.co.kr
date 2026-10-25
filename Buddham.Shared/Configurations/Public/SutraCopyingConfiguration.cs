using Buddham.Core.Entities.Public;
using Buddham.Shared.Constants;
using Buddham.Shared.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buddham.Shared.Configurations.Public;

public class SutraCopyingConfiguration : IEntityTypeConfiguration<SutraCopying>, IPublicConfiguration
{
    public void Configure(EntityTypeBuilder<SutraCopying> builder)
    {
        builder.ToTable(TableNames.SutraCopying, SchemaTypes.Public);
        builder.HasKey(x => x.Id);

        // 기본 컬럼
        builder.Property(x => x.Id).UseIdentityByDefaultColumn();
        builder.Property(x => x.Title).IsRequired().HasMaxLength(500).HasColumnType(PgTypes.Text);
        builder.Property(x => x.Sutra).HasColumnType(PgTypes.Text);
        builder.Property(x => x.UserId).IsRequired().HasColumnType(PgTypes.Uuid);
        builder.Property(x => x.CreatedAt).IsRequired().HasDefaultValueSql("NOW()");

        // tsvector
        builder.Property(x => x.SearchVector)
            .HasColumnType(PgTypes.TsVector)
            .HasConversion(EfConverters.TsVectorConverter)
            .HasComputedColumnSql(
                "setweight(to_tsvector('simple', coalesce(title, '')), 'A') ||" +
                "setweight(to_tsvector('simple', coalesce(sutra, '')), 'B')",
                 stored: true
            )
            .ValueGeneratedOnAddOrUpdate()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        // vector
        builder.Property(x => x.Embedding)
            .HasColumnType("vector(1024)")
            .HasConversion(EfConverters.VectorConverter);

        builder.HasIndex(x => new { x.CreatedAt, x.Id })
            .IsDescending(true, true)
            .HasDatabaseName(IndexNames.SutraCopyingsCreatedAtIdDesc);

        // GIN 인덱스 추출 호출
        builder.HasGinSearchIndex(x => x.SearchVector, IndexNames.SutraCopyingsSearchVector);

        // HNSW 인덱스 추출 호출
        builder.HasHnswVectorIndex(x => x.Embedding, IndexNames.SutraCopyingsEmbedding);

        // UserId 인덱스 (유저별 조회 최적화 용)
        builder.HasIndex(x => x.UserId).HasDatabaseName(IndexNames.SutraCopyingsUserId);
    }
}
