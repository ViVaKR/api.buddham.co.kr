using Buddham.Core.Entities.Auth;
using Buddham.Core.Schemas;

namespace Buddham.Shared.Extensions.Auth;

public static class UserMapping
{
    extension(IQueryable<ApplicationUser> query)
    {
        /// <summary>
        /// 목록
        /// AsSummary()
        /// </summary>
        public IQueryable<UserSchema.Response> AsSummary()
        {
            return query.Select(user => new UserSchema.Response
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                FullName = user.FullName,
                Pseudonym = user.Pseudonym,
                Avatar = user.Avatar,
                TrustedDevicceKey = user.TrustedDeviceKey,
                EmailConfirmed = user.EmailConfirmed,
                AccessFailedCount = user.AccessFailedCount,
                TwoFactorEnabled = user.TwoFactorEnabled,
                Provider = user.Provider,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                IsDeleted = user.IsDeleted,
                LockoutEnd = user.LockoutEnd,
                LockoutEnabled = user.LockoutEnabled,
                TimeZone = user.TimeZone,
                Locale = user.Locale,
                IsActive = user.IsActive,
                LastLoginAt = user.LastLoginAt,
                LastLoginIP = user.LastLoginIp,
                Introduction = user.Introduction,
                Roles = user.UserRoles.Select(ur => new UserRoleSchema.UserRoleBrief
                {
                    En = ur.Role.Name!,
                    Ko = ur.Role.KoName ?? ur.Role.Name!
                })
            });
        }
    }

    extension(ApplicationUser user)
    {
        /// <summary>
        /// 수정
        /// ApplyPatch()
        public void ApplyPatch(UserSchema.Request request)
        {
            user.Provider = request.Provider;
            user.TimeZone = request.TimeZone ?? user.TimeZone;
            user.Locale = request.Locale ?? user.Locale;
            user.UpdatedAt = DateTime.UtcNow;
            user.Introduction = request.Introduction;
        }
    }

    extension(UserSchema.Request request)
    {
        /// <summary>
        /// 생성 (관리목적, 별도 회원가입 로직있음)
        /// ToEntity
        /// </summary>
        public ApplicationUser ToEntity()
        {
            return new ApplicationUser
            {
                Id = Guid.CreateVersion7(),
                UserName = request.Email,
                Email = request.Email,
                FullName = request.FullName,
                Pseudonym = request.Pseudonym,
                EmailConfirmed = false,
                Avatar = "buddha.png",
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}