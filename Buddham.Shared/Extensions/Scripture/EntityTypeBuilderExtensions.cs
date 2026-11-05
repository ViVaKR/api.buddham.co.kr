using System.Linq.Expressions;
using Buddham.Core.Common.Primitives;
using Buddham.Core.Entities.Scriptures;
using Buddham.Core.Interfaces.Scripture;
using Buddham.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buddham.Shared.Extensions.Scripture;

/// <summary>
/// Fluent Helper
/// 테이블, 키, 임베딩 부분, 유니크 등
/// </summary>
public static class EntityTypeBuilderExtensions
{
    private const string ActiveRowFilter = "deleted_at IS NULL";

    /// <summary>
    /// 테이블명, 스키마, PK(identity), 감사 컬럼 공통 설정
    /// </summary>
    public static EntityTypeBuilder<T> ConfigureEntityBase<T>(this EntityTypeBuilder<T> builder, string table, string schemaType)
        where T : EntityBase
    {
        builder.ToTable(table, schemaType);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityByDefaultColumn();

        // 저장 인터셉터가 CreateAt을 채우면 그 값이 쓰이고, 비어 있으면 DB 기본값이 채웁니다.
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        return builder;
    }

    /// <summary>
    /// Code + 이름 3종 컬럼의 길이, 필수 여부 공통 설정
    /// </summary>
    public static EntityTypeBuilder<T> ConfigureCodedName<T>(this EntityTypeBuilder<T> builder)
        where T : class, ICodedName
    {
        builder.Property(x => x.Code).IsRequired().HasMaxLength(100);
        builder.Property(x => x.NameKo).IsRequired().HasMaxLength(300);
        builder.Property(x => x.NameOriginal).HasMaxLength(500);
        builder.Property(x => x.NameRoman).HasMaxLength(500);
        return builder;
    }

    /// <summary>
    /// 소프트 삭제를 고려한 유니크 인덱스 (삭제되지 않은 행에만 적용)
    /// 삭제된 행의 Code를 다시 쓸 수 있습니다.
    /// </summary>
    public static IndexBuilder<T> HasActiveUniqueIndex<T>(
        this EntityTypeBuilder<T> builder,
        Expression<Func<T, object?>> keys,
        string name
    ) where T : class
    => builder.HasIndex(keys)
        .IsUnique()
        .HasFilter(ActiveRowFilter)
        .HasDatabaseName(name);

    public static EntityTypeBuilder<T> ConfigureEmbedding<T>(
        this EntityTypeBuilder<T> builder,
        Expression<Func<T, float[]?>> property,
        string indexName,
        int? m = null,
        int? efConstruction = null
    ) where T : class
    {
        var prop = builder.Property(property)
            .HasColumnType(ScriptureConstants.EmbeddingColumnType)
            .HasConversion(EfConverters.VectorConverter);

        var index = builder.HasIndex(prop.Metadata.Name)
            .HasMethod(PgMethods.HNSW)
            .HasOperators("vector_cosine_ops")
            .HasDatabaseName(indexName);

        // 지정하지 않으면 pgvector 기본값 (m=16, ef_construction=64)을 씁니다.
        if (m is not null) index.HasStorageParameter("m", m.Value);
        if (efConstruction is not null) index.HasStorageParameter("ef_construction", efConstruction.Value);

        return builder;
    }
}
