namespace Buddham.Core.Common.Options;

public class JwtOptions
{
    public const string SectionName = "JwtConfig";
    public string Key { get; init; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessTokenIn { get; set; }
    public int RefreshTokenIn { get; set; }
}
