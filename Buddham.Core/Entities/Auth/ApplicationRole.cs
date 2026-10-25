using Buddham.Core.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Buddham.Core.Entities.Auth;

public class ApplicationRole : IdentityRole<Guid>, IAuditable, ISoftDeletable
{
    public ApplicationRole()
    {
    }

    public ApplicationRole(string roleName) : base(roleName)
    {
    }

    public string? KoName { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public bool IsSystem { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? Description { get; set; }
    public virtual ICollection<ApplicationUserRole> UserRoles { get; set; } = [];
}
