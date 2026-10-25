using System.Text;

namespace Buddham.Core.Common;

/// <summary>
/// 커서 토큰 생성기
/// 🚨 커서 토큰 유틸리티도 Guid 파츠로 완벽 동기화!
/// </summary>
public static class CursorToken
{
    /// <summary>
    /// Create (Guid)
    /// </summary>
    public static string Create(DateTime? sortValue, Guid id) // string 대신 Guid를 직접 흡입!
    {
        var ticks = sortValue?.Ticks ?? 0;
        var raw = $"{ticks}|{id}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
    }

    /// <summary>
    /// CretateToken (long)
    /// string 대신 Guid를 직접 흡입!
    /// </summary>
    public static string CreateToken(DateTime? sortValue, long id)
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
    public static string CreateVectorToken(int offset, string keyword)
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