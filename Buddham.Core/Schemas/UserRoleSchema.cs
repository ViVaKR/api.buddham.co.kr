namespace Buddham.Core.Schemas;

public static class UserRoleSchema
{
    public record Response
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public required string KoName { get; set; }
        public string? NormalizedName { get; set; }
        public string? Description { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
        public bool IsSystem { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
    }

    public record Request(
        Guid? Id,
        string Name,
        string KoName,
        string? Description,
        int SortOrder,
        bool IsActive,
        bool IsSystem
    );

    public record UserRoleBrief
    {
        public string En { get; init; } = string.Empty; // 예: "Admin"
        public string Ko { get; init; } = string.Empty; // 예: "관리자"
    }
}
