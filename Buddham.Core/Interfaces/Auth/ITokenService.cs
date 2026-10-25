using System.Security.Claims;
using Buddham.Core.Entities.Auth;

namespace Buddham.Core.Interfaces.Auth;

public interface ITokenService
{
    Task<string> GenerateAccessTokenAsync(ApplicationUser user);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
