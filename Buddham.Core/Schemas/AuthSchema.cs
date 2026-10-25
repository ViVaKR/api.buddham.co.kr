namespace Buddham.Core.Schemas;

public static class AuthSchema
{
    public record SignIn(
        string Email,
        string Password,
        string? DeviceFingerprint,
        string? IpAddress
    );

    public record SignUp(
        string FullName,
        string Pseudonym,
        string Email,
        string Password
    );

    public record CheckPassword(string Password);

    public record UpdateUser(
        string? FullName,
        string? Pseudonym,
        string? Avatar,
        string? Introduction,
        string? Provider,
        string? TimeZone,
        string? Locale
    );

    public record UpdateUserRoles(
        Guid Id,
        string[] Roles
    );

    public record UpdatePseudonym(string Id, string Pseudonym);

    public record UpdateFullName(string Id, string FullName);

    public record ResetPassword(string Email, string Token, string NewPassword);

    public record CheckPseudonym(string Pseudonym);

    public record CheckEmail(string Email);

    public record UserDetail
    {
        public Guid Id { get; init; }
        public required string FullName { get; init; }
        public required string Pseudonym { get; init; }
        public required string Email { get; init; }
        public bool EmailConfirmed { get; init; }
        public int AccessFailedCount { get; init; }
        public bool TwoFactorEnabled { get; init; }
        public string? Avatar { get; init; }
        public string? TrustedDevicceKey { get; set; }
        public string? Provider { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime? UpdatedAt { get; init; }
        public string? TimeZone { get; set; }
        public string? Locale { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public string? LastLoginIP { get; set; }
        public string? Introduction { get; init; }
        public IEnumerable<UserRoleInfo>? Roles { get; init; }
    }

    public record UserRoleInfo
    {
        public string? En { get; init; }
        public string? Ko { get; init; }
    }

    public record ChangePassword(
        string Email,
        string OldPassword,
        string NewPassword
    );

    public record Withdraw(string Password);

    public record DeleteAccountByAdmin(Guid UserId);

    public record TokenRefresh(string AccessToken, string RefreshToken);

    public record Identity
    {
        public Guid? UserId { get; init; }
        public string? Email { get; set; }
        public string? Token { get; init; }
        public string? RefreshToken { get; init; }
        public DateTime? RefreshTokenExpiry { get; init; }
        public bool EmailConfirmed { get; init; }
        public bool ShowEmailWarning { get; init; }
        public bool RequiresTwoFactor { get; init; }
        public bool? HasTwoFactorEnabled { get; init; }
    }
}
