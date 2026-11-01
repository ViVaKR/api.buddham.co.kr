using Buddham.Core.Entities.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Buddham.Shared.Seeds.Auth;

public static class SeedApplication
{
    public static async Task SeedRoleAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<WebApplication>>();
        var roles = new List<ApplicationRole>
        {
            new()
            {
                Id = Guid.CreateVersion7(),
                Name = "Admin",
                NormalizedName = "ADMIN",
                KoName = "관리자",
                SortOrder = 0,
                Description = "사이트 개발 및 경전 등록에 모든 권한을 소유합니다.",
                IsActive = true,
                IsSystem = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
            },
            new()
            {
                Id = Guid.CreateVersion7(),
                Name = "Monk",
                NormalizedName = "MONK",
                KoName = "스님회원",
                SortOrder = 20,
                Description = "정식 종단에 소속된 스님 회원으로 인증된 경전을 관리 권한을 가집니다.",
                IsActive = true,
                IsSystem = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
            },
            new()
            {
                Id = Guid.CreateVersion7(),
                Name = "Editor",
                KoName = "편집자",
                NormalizedName = "EDITOR",
                SortOrder = 40,
                Description = "경전 내용 편집 및 교정 권한을 가집니다.",
                IsActive = true,
                IsSystem = false,
                CreatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.CreateVersion7(),
                Name = "User",
                NormalizedName = "USER",
                KoName = "일반회원",
                SortOrder = 100,
                Description = "경전 읽기 및 사경 권한을 가집니다.",
                IsActive = true,
                IsSystem = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
            },
        };

        foreach (var role in roles)
        {
            var exists = await roleManager.RoleExistsAsync(role.Name!);
            if (exists)
            {
                logger.LogInformation("Role already exists: {Role}", role.Name);
                continue;
            }
            // role.NormalizedName = roleManager.KeyNormalizer.NormalizeName(role.Name);
            var result = await roleManager.CreateAsync(role);
            if (result.Succeeded)
            {
                logger.LogInformation("Role created: {Role}", role.Name);
            }
            else
            {
                var errors = string.Join(", ",
                    result.Errors.Select(e => e.Description));
                logger.LogError("Role creation failed: {Role} → {Errors}",
                    role.Name, errors);
            }
        }
    }
}
