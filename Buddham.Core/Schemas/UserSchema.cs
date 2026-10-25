using Buddham.Core.Common.Primitives;

namespace Buddham.Core.Schemas;

public static class UserSchema
{
    public record Response
    {
        public Guid Id { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public required string FullName { get; set; }
        public required string Pseudonym { get; set; }
        public string? Avatar { get; set; }
        public string? TrustedDevicceKey { get; set; }
        public bool EmailConfirmed { get; set; }
        public int AccessFailedCount { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public string? Provider { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public bool LockoutEnabled { get; set; }
        public string? TimeZone { get; set; }
        public string? Locale { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public string? LastLoginIP { get; set; }
        public string? Introduction { get; set; }
        public IEnumerable<UserRoleSchema.UserRoleBrief> Roles { get; set; } = [];
    }

    public record Request
    {
        public Guid? Id { get; set; }
        public required string UserName { get; set; }
        public required string FullName { get; set; }
        public required string Pseudonym { get; set; }
        public required string Email { get; set; }
        public string? Avatar { get; set; }
        public string? Provider { get; set; }
        public string? PhoneNumber { get; set; }
        public string? TimeZone { get; set; }
        public string? Locale { get; set; }
        public bool IsActive { get; set; }
        public string? LastLoginIp { get; set; }
        public string? Introduction { get; set; }
    }
}