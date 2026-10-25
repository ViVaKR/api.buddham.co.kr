namespace Buddham.Core.Common.Options;

public class AuthenticationOptions
{
    public OAuthProviderOptions Google { get; init; } = null!;
    public OAuthProviderOptions GitHub { get; init; } = null!;
    public OAuthProviderOptions Microsoft { get; init; } = null!;
}