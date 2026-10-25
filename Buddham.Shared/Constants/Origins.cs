using System.Text.RegularExpressions;

namespace Buddham.Shared.Constants;

public static partial class Origins
{
    /// <summary>
    /// 도메인:
    /// buddham.co.kr,
    /// kimbumjun.com,
    /// kimbumjun.co.kr,
    /// text.or.kr,
    /// vivabm.com,
    /// vivakr.com,
    /// writer.or.kr
    /// 로 끝나고 앞에 서브도메인이 붙거나 안 붙거나(https://)
    ///  로컬 도메인:
    /// http://localhost:포트번호 또는 http://127.0.0.1:포트번호 
    /// </summary>
    public static readonly Regex AllowedOriginsRegex = MyRegex();

    [GeneratedRegex(@"^https:\/\/(?:[a-z0-9-]+\.)*(?:buddham\.co\.kr|kimbumjun\.com|kimbumjun\.co\.kr|text\.or\.kr|vivabm\.com|vivakr\.com|writer\.or\.kr)$|^http:\/\/(?:localhost|127\.0\.0\.1):\d+$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex MyRegex();
}