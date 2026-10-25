using Buddham.Core.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Buddham.Core.Entities.Auth;

public class ApplicationUser: IdentityUser<Guid>, IAuditable, ISoftDeletable
{
    public required string FullName { get; set; }
    public required string Pseudonym { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
    public string? TrustedDeviceKey { get; set; }
    public DateTime? TrustedDeviceExpiry { get; set; }
    public string? Introduction { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public string? LastLoginIp { get; set; }
    public string? Provider { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string TimeZone { get; set; } = "Asia/Seoul";
    public string Locale { get; set; } = "ko-KR";
    public bool IsDeleted => DeletedAt.HasValue;
    public uint RowVersion { get; set; }
    public virtual ICollection<ApplicationUserRole> UserRoles { get; set; } = [];
    public DateTime? DeletedAt { get; set; } 
}
