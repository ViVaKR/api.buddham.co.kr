using Buddham.Core.Entities.Auth;
using Buddham.Core.Schemas;

namespace Buddham.Shared.Extensions.Auth;

public static class UserExtensions
{
    extension(IQueryable<ApplicationUser> query)
    {
        /// <summary>
        /// 목록 제공용
        /// AsSummary()
        /// </summary>
        public IQueryable<AuthSchema.UserDetail> ToDTO()
        {
            return query.Select(x => new AuthSchema.UserDetail
            {
                Id = x.Id,
                FullName = x.FullName,
                Pseudonym = x.Pseudonym,
                Email = x.Email!,
                EmailConfirmed = x.EmailConfirmed,
                TwoFactorEnabled = x.TwoFactorEnabled,
                AccessFailedCount = x.AccessFailedCount,
                Avatar = x.Avatar,
                TrustedDevicceKey = x.TrustedDeviceKey,
                Provider = x.Provider,
                Locale = x.Locale,
                TimeZone = x.TimeZone,
                Roles = x.UserRoles.Select(ur => new AuthSchema.UserRoleInfo
                {
                    En = ur.Role.Name!,
                    Ko = ur.Role.KoName ?? ur.Role.Name!
                }),
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                IsActive = x.IsActive,
                LastLoginAt = x.LastLoginAt,
                LastLoginIP = x.LastLoginIp,
                Introduction = x.Introduction
            });
        }
    }

    extension(AuthSchema.SignUp dto)
    {
        /// <summary>
        /// 회원가입
        /// </summary>
        public ApplicationUser ToEntity()
        {
            return new ApplicationUser
            {
                Id = Guid.CreateVersion7(),
                UserName = dto.Email,
                Email = dto.Email,
                FullName = dto.FullName,
                Pseudonym = dto.Pseudonym,
                EmailConfirmed = false,
                Avatar = "buddha.png",
                CreatedAt = DateTime.UtcNow,
                LockoutEnabled = true
            };
        }
    }

    extension(ApplicationUser user)
    {
        /// <summary>
        /// 회원정보 수정
        /// </summary>
        public void ToPatch(AuthSchema.UpdateUser dto)
        {
            user.Provider = dto.Provider ?? user.Provider;
            user.TimeZone = dto.TimeZone ?? user.TimeZone;
            user.Locale = dto.Locale ?? user.Locale;
            user.UpdatedAt = DateTime.UtcNow;
            user.Introduction = dto.Introduction;
        }

        /// <summary>
        ///     로그인 후 제공 정보용
        /// </summary>
        public AuthSchema.Identity ToIdentity(string token,
            string refreshToken,
            DateTime? expiry,
            bool emailConfirmed,
            bool has2Fa
        )
        {
            return new AuthSchema.Identity
            {
                UserId = user.Id,
                Email = user.Email,
                Token = token,
                RefreshToken = refreshToken,
                RefreshTokenExpiry = expiry,
                EmailConfirmed = emailConfirmed,
                ShowEmailWarning = !emailConfirmed,
                RequiresTwoFactor = false,
                HasTwoFactorEnabled = has2Fa
            };
        }

        /// <summary>
        ///     개별 회원정보
        /// </summary>
        public AuthSchema.UserDetail ToDetail()
        {
            return new AuthSchema.UserDetail
            {
                Id = user.Id,
                FullName = user.FullName,
                Pseudonym = user.Pseudonym,
                Email = user.Email!,
                EmailConfirmed = user.EmailConfirmed,
                AccessFailedCount = user.AccessFailedCount,
                Avatar = user.Avatar,
                TrustedDevicceKey = user.TrustedDeviceKey,
                TwoFactorEnabled = user.TwoFactorEnabled,
                Introduction = user.Introduction,
                Provider = user.Provider,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                TimeZone = user.TimeZone,
                Locale = user.Locale,
                Roles = user.UserRoles.Select(ur => new AuthSchema.UserRoleInfo
                {
                    En = ur.Role.Name!,
                    Ko = ur.Role.KoName ?? ur.Role.Name!
                }).ToList()
            };
        }
    }
}