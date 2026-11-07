using Buddham.Core.Common.Results;

namespace Buddham.Shared.Services.Crud;

// ══════════════════════════════════════════════════════════
// 4) 요청 검증: 규칙을 체이닝으로 선언
// ══════════════════════════════════════════════════════════

public sealed class Validator
{
    private readonly Dictionary<string, string[]> _errors = new();

    /// <param name="trimForLength">true 면 앞뒤 공백을 뺀 길이로 검사 (제목류), false 면 원문 길이 (본문류).</param>
    public Validator Text(string field, string? value, string label, int maxLength,
        bool required = true, bool trimForLength = true)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            if (required) _errors[field] = [$"{label}{Topic(label)} 필수입니다."];
            return this;
        }

        var length = trimForLength ? value.Trim().Length : value.Length;
        if (length > maxLength)
            _errors[field] = [$"{label}{Topic(label)} {maxLength}자 이하여야 합니다."];

        return this;
    }

    public Validator EnumDefined<TEnum>(string field, TEnum value, string label) where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
            _errors[field] = [$"유효하지 않은 {label}입니다."];
        return this;
    }

    public Validator Must(bool condition, string field, string message)
    {
        if (!condition) _errors[field] = [message];
        return this;
    }

    public Result? ToResult() => _errors.Count == 0 ? null : Result.ValidationFailed(_errors);

    /// <summary>받침 유무로 은/는 선택.</summary>
    private static string Topic(string label)
    {
        var c = label[^1];
        if (c is < '\uAC00' or > '\uD7A3') return "은";
        return (c - '\uAC00') % 28 == 0 ? "는" : "은";
    }
}
