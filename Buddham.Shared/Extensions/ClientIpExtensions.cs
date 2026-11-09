using Microsoft.AspNetCore.Http;

namespace Buddham.Shared.Extensions;

public static class ClientIpExtensions
{
    /// <summary>
    /// 클라이언트 IP. RemoteIpAddress 만 신뢰한다.
    ///
    /// X-Forwarded-For / X-Real-IP 헤더를 직접 읽으면 누구나 값을 위조할 수 있다.
    /// 프록시 뒤에서는 UseForwardedHeaders(신뢰 프록시 지정)가 RemoteIpAddress 를
    /// 실제 클라이언트 IP 로 바꿔 주므로, 애플리케이션 코드는 이 값만 쓰면 된다.
    /// </summary>
    public static string? GetClientIp(this HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress;
        if (ip is null) return null;
        return ip.IsIPv4MappedToIPv6 ? ip.MapToIPv4().ToString() : ip.ToString();
    }
}
