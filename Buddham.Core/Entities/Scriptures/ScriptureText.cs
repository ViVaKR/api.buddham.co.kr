using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Buddham.Core.Scriptures;

/// <summary>
/// 경전 텍스트에 관한 순수 규칙. 인프라(EF, HTTP 등)에 의존하지 않는다.
/// </summary>
public static partial class ScriptureText
{
    public const int MaxRefLength = 100;

    [GeneratedRegex("^[a-z0-9][a-z0-9._-]{0,99}$")]
    private static partial Regex CodePattern();

    [GeneratedRegex(@"^[a-z]{2,3}(-[A-Za-z0-9]{2,8})*$")]
    private static partial Regex LanguagePattern();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRun();

    // ───────── 식별자·코드 ─────────

    /// <summary>NFC 정규화 + 앞뒤 공백 제거.</summary>
    public static string Clean(string? s)
        => (s ?? string.Empty).Normalize(NormalizationForm.FormC).Trim();

    /// <summary>빈 문자열은 null 로.</summary>
    public static string? Blank(string? s)
    {
        var c = Clean(s);
        return c.Length == 0 ? null : c;
    }

    /// <summary>코드: 소문자 영숫자 + . _ - (예: sn, sn-12, agama.za)</summary>
    public static bool TryNormalizeCode(string? raw, [NotNullWhen(true)] out string? code)
    {
        var c = Clean(raw).ToLowerInvariant();
        code = CodePattern().IsMatch(c) ? c : null;
        return code is not null;
    }

    /// <summary>언어 코드: pi, lzh, ko, bo, sa, en, zh-Hant ...</summary>
    public static bool TryNormalizeLanguage(string? raw, [NotNullWhen(true)] out string? language)
    {
        language = null;
        var parts = Clean(raw).Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return false;

        for (var i = 0; i < parts.Length; i++)
        {
            parts[i] = i == 0 ? parts[i].ToLowerInvariant()
                : parts[i].Length == 4 ? char.ToUpperInvariant(parts[i][0]) + parts[i][1..].ToLowerInvariant()
                : parts[i].Length == 2 ? parts[i].ToUpperInvariant()
                : parts[i].ToLowerInvariant();
        }

        var joined = string.Join('-', parts);
        if (joined.Length > 16 || !LanguagePattern().IsMatch(joined)) return false; // canon_editions.language_code = varchar(16)
        language = joined;
        return true;
    }

    /// <summary>문자 체계: Latn, Hani, Hang, Deva, Tibt ... (비어 있으면 null 허용)</summary>
    public static bool TryNormalizeScript(string? raw, out string? script)
    {
        var c = Clean(raw);
        if (c.Length == 0) { script = null; return true; }
        if (c.Length != 4 || !c.All(char.IsLetter)) { script = null; return false; }
        script = char.ToUpperInvariant(c[0]) + c[1..].ToLowerInvariant();
        return true;
    }

    /// <summary>세그먼트 주소(SegmentRef): 판본 안에서 유일한 문자열, 예) "12.2:3", "p.45-l.3"</summary>
    public static bool TryNormalizeRef(string? raw, [NotNullWhen(true)] out string? segmentRef)
    {
        var c = Clean(raw);
        segmentRef = c.Length is >= 1 and <= MaxRefLength && !c.Any(char.IsControl) ? c : null;
        return segmentRef is not null;
    }

    /// <summary>경 식별자 값: NFC, 공백 압축. 대소문자는 보존(비교는 대소문자 무시).</summary>
    public static string NormalizeIdentifier(string? raw)
        => WhitespaceRun().Replace(Clean(raw), " ");

    // ───────── 검색용 정규화 / 해시 ─────────

    /// <summary>
    /// 검색용 정규화(text_normalized): 소문자, 구두점 제거, 공백 압축.
    /// 라틴 문자의 발음구별 기호(ā ṭ ṃ ñ ...)는 제거하여 "satipatthana" 로도 찾게 한다.
    /// 데바나가리·티베트 등 비라틴 문자의 결합 기호는 의미가 있으므로 보존한다.
    /// 한글은 NFD→NFC 왕복으로 온전히 복원된다.
    /// </summary>
    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var decomposed = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        var prevLatin = false;
        var lastSpace = true;

        foreach (var ch in decomposed)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (cat is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark or UnicodeCategory.EnclosingMark)
            {
                if (!prevLatin) sb.Append(ch);
                continue;
            }

            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(char.ToLowerInvariant(ch));
                prevLatin = IsLatin(ch);
                lastSpace = false;
            }
            else
            {
                prevLatin = false;
                if (!lastSpace) { sb.Append(' '); lastSpace = true; }
            }
        }

        return sb.ToString().Trim().Normalize(NormalizationForm.FormC);
    }

    private static bool IsLatin(char c) => c <= '\u024F' || c is >= '\u1E00' and <= '\u1EFF';

    public static string Sha256Hex(string s)
        => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(s)));

    /// <summary>세그먼트 본문 해시(text_hash). 변경 감지용.</summary>
    public static string TextHash(string? text) => Sha256Hex(Clean(text));

    /// <summary>판본 전체 해시(content_hash): (ref, textHash) 를 순서대로 이어 붙인 해시.</summary>
    public static string ContentHash(IEnumerable<(string Ref, string TextHash)> ordered)
    {
        var sb = new StringBuilder();
        foreach (var (r, h) in ordered) sb.Append(r).Append('\t').Append(h).Append('\n');
        return Sha256Hex(sb.ToString());
    }

    /// <summary>LIKE 패턴용 이스케이프(\ % _).</summary>
    public static string EscapeLike(string s)
        => s.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    // ───────── 사경 정확도 ─────────

    /// <summary>
    /// 사경 정확도 0..1. 사경은 원문 그대로 옮기는 수행이므로 발음 기호를 제거하지 않고,
    /// 공백 차이와 유니코드 정규형만 무시한 채 글자 단위 편집거리로 비교한다.
    /// </summary>
    public static double CopyAccuracy(string? expected, string? typed)
    {
        var a = WhitespaceRun().Replace(Clean(expected), " ");
        var b = WhitespaceRun().Replace(Clean(typed), " ");
        if (a.Length == 0) return b.Length == 0 ? 1.0 : 0.0;
        var distance = Levenshtein(a, b);
        return Math.Max(0.0, 1.0 - (double)distance / Math.Max(a.Length, b.Length));
    }

    private static int Levenshtein(string a, string b)
    {
        if (a.Length == 0) return b.Length;
        if (b.Length == 0) return a.Length;

        var prev = new int[b.Length + 1];
        var curr = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++) prev[j] = j;

        for (var i = 1; i <= a.Length; i++)
        {
            curr[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                curr[j] = Math.Min(Math.Min(curr[j - 1] + 1, prev[j] + 1), prev[j - 1] + cost);
            }
            (prev, curr) = (curr, prev);
        }
        return prev[b.Length];
    }
}
