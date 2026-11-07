using System.Text;
using System.Text.Json;

namespace Buddham.Core.Common;

/// <summary>
/// 커서 토큰 생성기
/// 🚨 커서 토큰 유틸리티도 Guid 파츠로 완벽 동기화!
/// </summary>
public static class CursorToken
{
    private const int CurrentVersion = 1;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    public sealed record PageCursorPayload
    {
        public int Version { get; init; } = CurrentVersion;
        public long Ticks { get; init; }
        public long Id { get; init; }
        public string? Keyword { get; init; }
        public bool Descending { get; init; }
    }

    public sealed record VectorCursorPayload
    {
        public int Version { get; init; } = CurrentVersion;
        public int Offset { get; init; }
        public string Keyword { get; init; } = string.Empty;
        public float Threshold { get; init; }
    }

    // private static
    /// <summary>
    /// Create (Guid)
    /// </summary>
    public static string CreateTokenByGuid(DateTime? sortValue, Guid id) // string 대신 Guid를 직접 흡입!
    {

        var ticks = sortValue?.Ticks ?? 0;
        var raw = $"{ticks}|{id}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
    }

    public static string CreateToken(
        DateTime? sortValue,
        long id,
        string? keyword,
        bool descending)
    {

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        return Encode(new PageCursorPayload
        {
            Ticks = sortValue?.Ticks ?? 0,
            Id = id,
            Keyword = NormalizeKeyword(keyword),
            Descending = descending
        });
    }
    public static bool TryParseToken(string? token, out PageCursorPayload? payload)
    {
        payload = Decode<PageCursorPayload>(token);
        return payload is not null
            && payload.Version == CurrentVersion
            && payload.Ticks > 0
            && payload.Ticks <= DateTime.MaxValue.Ticks
            && payload.Id > 0;
    }

    public static string CreateVectorToken(int offset, string keyword, float threshold)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);

        if (string.IsNullOrWhiteSpace(keyword)) throw new ArgumentException("검색어가 필요합니다.", nameof(keyword));

        if (!float.IsFinite(threshold) || threshold is < 0f or > 1f)
            throw new ArgumentOutOfRangeException(nameof(threshold));


        return Encode(new VectorCursorPayload
        {
            Offset = offset,
            Keyword = keyword.Trim(),
            Threshold = threshold
        });
    }

    public static bool TryParseVectorToken(string? token, out VectorCursorPayload? payload)
    {
        payload = Decode<VectorCursorPayload>(token);
        return payload is not null
            && payload.Version == CurrentVersion
            && payload.Offset >= 0
            && !string.IsNullOrWhiteSpace(payload.Keyword)
            && float.IsFinite(payload.Threshold)
            && payload.Threshold is >= 0f and <= 1f;
    }

    private static string Encode<T>(T payload)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions);
        return Convert.ToBase64String(json)
            .Trim('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static T? Decode<T>(string? token) where T : class
    {
        if (string.IsNullOrWhiteSpace(token)) return null;

        try
        {
            var base64 = token.Replace('-', '+').Replace('_', '/');
            base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');
            var bytes = Convert.FromBase64String(base64);
            return JsonSerializer.Deserialize<T>(bytes, JsonOptions);
        }
        catch (Exception ex) when (ex is FormatException or JsonException or ArgumentException)
        {
            return null;
        }
    }

    private static string? NormalizeKeyword(string? keyword) =>
        string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim();


    /// <summary>
    /// CretateToken (long)
    /// string 대신 Guid를 직접 흡입!
    /// </summary>
    public static string CreateTokenByNumber(DateTime? sortValue, long id)
    {
        var ticks = sortValue?.Ticks ?? 0;
        var raw = $"{ticks}|{id}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
    }

    /// <summary>
    /// Parse (Guid)
    /// </summary>
    public static (long Ticks, Guid Id) Parse(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return (0, Guid.Empty);
        try
        {
            var bytes = Convert.FromBase64String(token);
            var raw = Encoding.UTF8.GetString(bytes);
            var parts = raw.Split('|');
            if (parts.Length == 2 && long.TryParse(parts[0], out var ticks) && Guid.TryParse(parts[1], out var id))
                return (ticks, id);
        }
        catch
        {
            // ignored
            return (0, Guid.Empty);
        }

        return (0, Guid.Empty);
    }

    /// <summary>
    /// ParseToken (long)
    /// </summary>
    public static (long Ticks, long Id) ParseToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return (0, 0);
        try
        {
            var bytes = Convert.FromBase64String(token);
            var raw = Encoding.UTF8.GetString(bytes);
            var parts = raw.Split('|');
            if (parts.Length == 2 && long.TryParse(parts[0], out var ticks) && long.TryParse(parts[1], out var id))
                return (ticks, id);
        }
        catch
        {
            // ignored
        }

        return (0, 0);
    }

    /// <summary>
    /// Create
    /// 백터 검색용 : Offset 기반 커서
    /// </summary>
    public static string CreateVectorToken(int offset, string? keyword)
    {
        var raw = $"v:{offset}:{keyword}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
    }

    /// <summary>
    /// Parse
    /// 백터 검색용 : Offset 기반 커서
    /// </summary>
    public static (bool isVector, int offset, string) ParseVectorToken(string cursor)
    {
        try
        {
            var raw = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            if (!raw.StartsWith("v:")) return (false, 0, string.Empty);

            var parts = raw[2..].Split(':', 2);
            return (true, int.Parse(parts[0]), parts[1]);
        }
        catch
        {
            return (false, 0, string.Empty);
        }
    }
}
