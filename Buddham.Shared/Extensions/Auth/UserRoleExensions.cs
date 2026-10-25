using Buddham.Core.Entities.Auth;
using Buddham.Core.Schemas;

namespace Buddham.Shared.Extensions.Auth;

public static class UserRoleExensions
{
    extension(IQueryable<ApplicationRole> query)
    {
        /// <summary>
        /// 목록
        /// List
        /// AsSummary()
        /// </summary>
        public IQueryable<UserRoleSchema.Response> AsSummary()
        {
            return query.Select(x => new UserRoleSchema.Response
            {
                Id = x.Id,
                KoName = x.KoName ?? string.Empty,
                Name = x.Name ?? string.Empty,
                NormalizedName = x.NormalizedName,
                Description = x.Description,
                SortOrder = x.SortOrder,
                IsActive = x.IsActive,
                IsSystem = x.IsSystem,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
            });
        }
    }

    extension(ApplicationRole role)
    {
        /// <summary>
        /// 단건 조회
        /// AsDetail()
        /// </summary>
        public UserRoleSchema.Response AsDetail()
        {
            return new UserRoleSchema.Response
            {
                Id = role.Id,
                KoName = role.KoName ?? string.Empty,
                Name = role.Name ?? string.Empty,
                Description = role.Description,
                SortOrder = role.SortOrder,
                IsActive = role.IsActive,
                IsSystem = role.IsSystem,
                CreatedAt = role.CreatedAt,
                UpdatedAt = role.UpdatedAt,
            };
        }

        /// <summary>
        /// 수정
        /// ApplyPatch()
        /// </summary>
        public void ApplyPatch(UserRoleSchema.Request request)
        {
            // ========================================================
            // 🔥 [영문 이름 정체성 사수 방어막]
            // 시스템 필수 역할(Admin 등) 고유의 영문 고유 Name은 수정을 원천 차단!
            // ========================================================
            if (!role.IsSystem) role.Name = request.Name;
            role.KoName = request.KoName;
            role.Description = request.Description;
            role.SortOrder = request.SortOrder;
            role.IsActive = request.IsActive;
            role.IsSystem = request.IsSystem;
            role.UpdatedAt = DateTime.UtcNow;
        }
    }

    extension(UserRoleSchema.Request request)
    {
        /// <summary>
        /// 생성
        /// ToEntity()
        /// </summary>
        public ApplicationRole ToEntity()
        {
            return new ApplicationRole
            {
                Id = Guid.CreateVersion7(),
                Name = request.Name,
                KoName = request.KoName,
                Description = request.Description,
                SortOrder = request.SortOrder,
                IsActive = request.IsActive,
                IsSystem = request.IsSystem,
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}