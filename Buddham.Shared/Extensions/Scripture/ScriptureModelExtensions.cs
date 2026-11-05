using Buddham.Core.EnumTypes;
using Microsoft.EntityFrameworkCore;

namespace Buddham.Shared.Extensions.Scripture;

public static class ScriptureModelExtensions
{
    public static ModelConfigurationBuilder ConfigureScriptureConventions(this ModelConfigurationBuilder builder)
    {
        var ns = typeof(DivisionKind).Namespace;
        foreach (var enumType in typeof(DivisionKind).Assembly.GetTypes().Where(t => t.IsEnum && t.Namespace == ns))
            builder.Properties(enumType).HaveConversion<string>().HaveMaxLength(32);

        return builder;
    }

    /// <summary>
    /// 지정한 네임스페이스 엔터티의 모든 FK 를 Restrict 로 맞춥니다.
    /// 소프트 삭제를 쓰므로, 실수로 하드 삭제가 연쇄되지 않게 막기 위함입니다.
    /// </summary>
    public static ModelBuilder UseRestrictDeleteFor(this ModelBuilder builder, string? entityNamespace)
    {
        var foreignKeys = builder.Model.GetEntityTypes()
            .Where(x => x.ClrType.Namespace == entityNamespace)
            .SelectMany(x => x.GetDeclaredForeignKeys())
            .ToList();

        foreach (var fk in foreignKeys)
            fk.DeleteBehavior = DeleteBehavior.Restrict;

        return builder;
    }
}
