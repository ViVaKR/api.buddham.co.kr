namespace Buddham.Core.Schemas;

public static class TwoFactorSchema
{
    public record Login
    {
        public required string Email { get; set; }
        public required string Code { get; set; }
        public bool RememberDevice { get; set; } = false;
        public string? DeviceFingerprint { get; set; }
    }

    public record Setup
    {
        public string? SharedKey { get; set; }
        public string? QrCodeImageUrl { get; set; }
        public string? AuthenticatorUri { get; set; }
    }

    public record Verify
    {
        public required string Code { get; set; }
    }

    public record VerifyResponse
    {
        public List<string> RecoveryCodes { get; set; } = [];
        public string? Message { get; set; }
    }

    public record RecoveryCodeLogin
    {
        public required string Email { get; set; }
        public string? RecoveryCode { get; set; }
    }

    public record Identity
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiry { get; set; }
        public bool EmailConfirmed { get; set; } = true;
        public bool ShowEmailWarning { get; set; } = false;
        public bool RequiresTwoFactor { get; set; } // 이번 로그인에서 2FA 필요한가?
        public bool? TwoFactorEnabled { get; set; } // 사용자가 2FA 활성화했는가? (정보용)
    }
}