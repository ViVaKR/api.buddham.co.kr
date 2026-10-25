using Buddham.Core.Common;
using Buddham.Core.Common.Results;
using Buddham.Core.Entities.Auth;
using Buddham.Core.Interfaces.Auth;
using Buddham.Core.Schemas;
using Buddham.Shared.Extensions;
using Buddham.Shared.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Buddham.Api.Controllers.Auth
{
    [ApiController]
    [Route("[controller]")]
    public class TwoFactorAuthController(
        ITwoFactorService twoFactorService,
        UserManager<ApplicationUser> userManager
    ) : BaseController
    {
        [Authorize]
        [HttpPost("Enable2fa")]
        public async Task<ActionResult<Result<TwoFactorSchema.Setup>>> EnableTwoFactor()
        {
            var user = await userManager.GetUserAsync(User);
            if (user is null)
                return Result<TwoFactorSchema.Setup>.Failure(RsCode.Unauthorized, "인증되지 않은 요청입니다.");
            return (await twoFactorService.EnableTwoFactorAsync(user)).ToActionResult(this);
        }

        [Authorize]
        [HttpPost("Verify2fa")]
        public async Task<ActionResult<Result<TwoFactorSchema.VerifyResponse>>> VerifyTwoFactor(
            [FromBody] TwoFactorSchema.Verify dto)
        {
            var user = await userManager.GetUserAsync(User);
            if (user is null)
                return Result<TwoFactorSchema.VerifyResponse>.Failure(RsCode.Unauthorized, "인증되지 않은 요청입니다.");
            return (await twoFactorService.VerifyTwoFactorAsync(dto, user)).ToActionResult(this);
        }

        /// <summary>
        /// 2FA 로그인
        /// </summary>
        [AllowAnonymous]
        [HttpPost("Login2fa")]
        public async Task<ActionResult<Result<TwoFactorSchema.Identity>>> LoginWithTwoFactor(
            [FromBody] TwoFactorSchema.Login dto)
            => (await twoFactorService.LoginWithTwoFactorAsync(dto)).ToActionResult(this);

        /// <summary>
        /// 복구코드를 사용한 로그인
        /// </summary>
        [AllowAnonymous]
        [HttpPost("LoginRecoveryCode")]
        public async Task<ActionResult<Result<TwoFactorSchema.Identity>>> LoginWithRecoveryCode(
            [FromBody] TwoFactorSchema.RecoveryCodeLogin dto)
        {
            var user = await userManager.GetUserAsync(User);
            if (user is null)
                return NotFound(Result<TwoFactorSchema.Identity>.Failure(RsCode.Unauthorized, "인증되지 않은 요청입니다."));
            return (await twoFactorService.LoginWithRecoveryCodeAsync(dto)).ToActionResult(this);
        }

        [Authorize]
        [HttpPost("Disable2fa")]
        public async Task<ActionResult<Result>> DisableTwoFactor()
        {
            var user = await userManager.GetUserAsync(User);
            if (user is null)
                return Result<TwoFactorSchema.Identity>.Failure(RsCode.Unauthorized, "인증되지 않은 요청입니다.");
            return Ok(await twoFactorService.DisableTwoFactorAsync(user));
        }
    }
}