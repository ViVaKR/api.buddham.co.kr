using System.Linq.Expressions;
using Buddham.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buddham.Shared.Extensions;

public static class EntiryTypeBuilderExtensions
{
    /// <summary>
    /// PostgreSQL GIN 검색 인덱스 공통 확장
    /// </summary>
    public static IndexBuilder<TEntity> HasGinSearchIndex<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, object?>> indexExpression,
        string indexName) where TEntity : class
    {
        return builder.HasIndex(indexExpression).HasMethod(PgMethods.GIN).HasDatabaseName(indexName);
    }

    /// <summary>
    /// PostgreSQL HNSW 벡터 인덱스 공통 확장
    /// </summary>
    public static IndexBuilder<TEntity> HasHnswVectorIndex<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, object?>> indexExpression,
        string indexName,
        string operators = "vector_cosine_ops") where TEntity : class
    {
        return builder.HasIndex(indexExpression).HasMethod(PgMethods.HNSW).HasOperators(operators).HasDatabaseName(indexName);
    }
}

/*

[사용법]

// GIN 인덱스 추출 호출
builder.HasGinSearchIndex(x => x.SearchVector, "ix_sutra_copying_search_vector");

// HNSW 인덱스 추출 호출
builder.HasHnswVectorIndex(x => x.Embedding, "ix_sutra_copying_embedding");

*/
