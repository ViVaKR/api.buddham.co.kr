using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Buddham.Core.Common.Options;
using Buddham.Core.Entities.Auth;
using Buddham.Core.Interfaces.Auth;
using Buddham.Shared.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Buddham.Shared.Services.Auth;

public class TokenService(
    IOptions<JwtOptions> jwtOptions,
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager) : ITokenService
{
    private readonly JwtOptions options = jwtOptions.Value;

    // 서명 키는 한 번만 만든다 (요청마다 바이트 배열/키 객체를 새로 만들 필요 없음, 스레드 안전)
    private readonly SymmetricSecurityKey _signingKey
        = new(Encoding.UTF8.GetBytes(jwtOptions.Value.Key));

    // --> (Create New Token) 토큰 생성
    public async Task<string> GenerateAccessTokenAsync(ApplicationUser user)
    {
        // var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key));
        // var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);
        var creds = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha512);

        var iss = options.Issuer;
        var aud = options.Audience;

        var now = DateTimeOffset.UtcNow;
        var iat = now.ToUnixTimeSeconds().ToString();
        var expiry = now.AddMinutes(options.AccessTokenIn);

        List<Claim> claims =
        [
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()), // 사용자 식별자
            new(JwtRegisteredClaimNames.NameId, user.Id.ToString()), // 사용자 식별자
            new(JwtRegisteredClaimNames.Email, user.Email!), // 이메일
            new(JwtRegisteredClaimNames.Name, user.FullName), // 사용자 이름
            new(JwtRegisteredClaimNames.PreferredUsername, user.Pseudonym), // 별칭
            new(JwtRegisteredClaimNames.Picture, user.Avatar ?? string.Empty), // 프로필 이미지
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // 토큰 고유 식별자
            new(JwtRegisteredClaimNames.Iat, iat, ClaimValueTypes.Integer64), // 발급 시각
            new(AppInfrastructure.SecurityStamp, user.SecurityStamp!), // 보안 상태 검증
            // 신뢰 장치 식별
            new(AppInfrastructure.TrustedDeviceKey, user.TrustedDeviceKey ?? string.Empty),
            // 신뢰 장치 만료
            new(AppInfrastructure.TrustedDeviceExpiry, user.TrustedDeviceExpiry?.ToString("O") ?? string.Empty)
        ];

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // ✅ 4) 역할 추가 (ClaimTypes.Role)
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        var roles = await userManager.GetRolesAsync(user);
        // claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        claims.AddRange(roles.Select(role => new Claim("role", role)));

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // ✅ 5) 사용자 개인 클레임 추가 (AspNetUserClaims)
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        var userClaims = await userManager.GetClaimsAsync(user);
        claims.AddRange(userClaims);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // ✅ 6) 역할의 클레임 추가 (AspNetRoleClaims) - 핵심!
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        foreach (var roleName in roles)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role == null) continue;
            var roleClaims = await roleManager.GetClaimsAsync(role);
            claims.AddRange(roleClaims);
        }

        /*
            4번 (Role): "너 누구야?" (군대로 치면 '계급')
            5번 (User Claim): "너만 가진 특기가 뭐야?" (개인 포상휴가증 같은 것)
            6번 (Role Claim): "그 계급이면 할 수 있는 게 뭐야?" (병장이면 '취침 시간 연장' 권한이 있는 것)
         */

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = iss,
            Audience = aud,
            NotBefore = now.UtcDateTime,
            Expires = expiry.UtcDateTime,
            SigningCredentials = creds
        };


        // 7) 토큰 생성/직렬화
        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(tokenDescriptor);
        return handler.WriteToken(token);
    }

    // --> (Get Principal From Expired Token) 만료된 토큰에서 클레임 추출
    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key)),
            ValidateLifetime = false,
            RoleClaimType = "role"
        }, out var securityToken);

        if (securityToken is not JwtSecurityToken jwtSecurityToken ||
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha512,
                StringComparison.InvariantCultureIgnoreCase))
        {
            throw new SecurityTokenException("잘못된 토큰입니다.");
        }

        return principal;
    }

    // --> (Create Refresh Token) 리프레시 토큰 생성
    public string GenerateRefreshToken()
        => Convert.ToHexString(RandomNumberGenerator.GetBytes(86));
}
