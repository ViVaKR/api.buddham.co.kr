using System.Linq.Expressions;
using Buddham.Core.Interfaces;
using Buddham.Shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace Buddham.Shared.Extensions;

public static class DbContextExtensions
{
    public static ModelBuilder RegisterModelCreating(this ModelBuilder builder, string defaultSchema)
    {
        builder.HasDefaultSchema(defaultSchema);

        builder.UseIdentityByDefaultColumns();
        builder.ApplyGlobalSoftDeleteFilter();

        builder.HasPostgresExtension(PgExtensions.BtreeGin);
        builder.HasPostgresExtension(PgExtensions.PgTrgm);
        builder.HasPostgresExtension(PgExtensions.Pgcrypto);
        builder.HasPostgresExtension(PgExtensions.Postgis);
        builder.HasPostgresExtension(PgExtensions.Vector);
        return builder;
    }

    /// <summary>
    /// ISoftDelete를 구현한 모든 엔티티에 전역 소프트 삭제 필터를 자동으로 주입
    /// </summary>
    public static void ApplyGlobalSoftDeleteFilter(this ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
            // ISoftDelete 인터페이스를 구현한 클래스인지 확인
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
                builder.Entity(entityType.ClrType)
                    .HasQueryFilter(CreateFilterExpression(entityType.ClrType));
    }

    /// <summary>
    ///  필터생성 람다식
    /// </summary>
    private static LambdaExpression CreateFilterExpression(Type type)
    {
        var parameter = Expression.Parameter(type, "e");
        var property = Expression.Property(parameter, nameof(ISoftDeletable.DeletedAt));
        var isNull = Expression.Equal(property, Expression.Constant(null, typeof(DateTime?)));
        return Expression.Lambda(isNull, parameter);
    }
}