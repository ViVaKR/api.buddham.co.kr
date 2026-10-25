using System.Text;
using Buddham.Core.Common;
using Buddham.Core.Common.Results;
using Buddham.Core.Entities.Auth;
using Buddham.Core.Interfaces.Auth;
using Buddham.Core.Schemas;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QRCoder;

namespace Buddham.Shared.Services.Public;

public class TwoFactorService(
    UserManager<ApplicationUser> userManager,
    ITokenService tokenService
) : ITwoFactorService
{
    public async Task<Result<TwoFactorSchema.Setup>> EnableTwoFactorAsync(ApplicationUser user)
    {
        // 1. Authenticator Key
        var unfomattedKey = await userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(unfomattedKey))
        {
            await userManager.ResetAuthenticatorKeyAsync(user);
            unfomattedKey = await userManager.GetAuthenticatorKeyAsync(user);
        }

        // 2. QR 코드용 URI 생성
        var email = await userManager.GetEmailAsync(user);
        var authentiatorUri = GenerateQrCodeUri(email!, unfomattedKey!);

        // 3. QR 코드 이미지 생성
        var qrCodeBase64 = GenerateQrCode(authentiatorUri);

        var result = new TwoFactorSchema.Setup
        {
            SharedKey = FormatKey(unfomattedKey!),
            QrCodeImageUrl = $"data:image/png;png;base64, {qrCodeBase64}",
            AuthenticatorUri = authentiatorUri
        };

        return Result<TwoFactorSchema.Setup>.Success(result, "QR 코드가 생성되었습니다.");
    }

    public async Task<Result<TwoFactorSchema.VerifyResponse>> VerifyTwoFactorAsync(
        [FromBody] TwoFactorSchema.Verify dto,
        ApplicationUser user)
    {
        // 코드 검증
        var isValid = await userManager.VerifyTwoFactorTokenAsync(
            user,
            userManager.Options.Tokens.AuthenticatorTokenProvider,
            dto.Code
        );

        if (!isValid)
            return Result<TwoFactorSchema.VerifyResponse>.Failure(RsCode.BadRequest, $"인증 코드가 올바르지 않습니다. {dto.Code}");

        // 2FA 활성화
        await userManager.SetTwoFactorEnabledAsync(user, true);

        // Recovery codes 생성
        var recoveryCodes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);

        var result = new TwoFactorSchema.VerifyResponse
        {
            RecoveryCodes = recoveryCodes!.ToList(),
            Message = "2단계 인증이 활성화되었습니다."
        };
        return Result<TwoFactorSchema.VerifyResponse>.Success(result, "2단계 인증 설정 완료");
    }

    /// <summary>
    /// 로그인 2FA
    /// </summary>
    public async Task<Result<TwoFactorSchema.Identity>> LoginWithTwoFactorAsync(
        [FromBody] TwoFactorSchema.Login dto)
    {
        var user = await userManager.FindByEmailAsync(dto.Email);
        if (user is null)
            return Result<TwoFactorSchema.Identity>.Failure(RsCode.Unauthorized, "사용자를 찾을 수 없습니다.");

        if (!user.TwoFactorEnabled)
            return Result<TwoFactorSchema.Identity>.Failure(RsCode.BadRequest, "2단계 인증이 활성화되어 있지 않습니다.");

        // ✅ 계정 잠금 확인
        if (await userManager.IsLockedOutAsync(user))
            return Result<TwoFactorSchema.Identity>.Failure(RsCode.Unauthorized, "계정이 잠겼습니다. 잠시 후 다시 시도하여 주세요");

        // ✅ 2FA 코드 검증 (UserManager 사용)
        var isValid = await userManager.VerifyTwoFactorTokenAsync(
            user,
            userManager.Options.Tokens.AuthenticatorTokenProvider,
            dto.Code
        );

        if (!isValid)
        {
            await userManager.AccessFailedAsync(user);
            return Result<TwoFactorSchema.Identity>.Failure(RsCode.Unauthorized, "잘못된 인증 시도입니다.");
        }

        // ✅ 성공 시 실패 횟수 리셋
        await userManager.ResetAccessFailedCountAsync(user);

        // 이전 인증 상태 무효화
        // 토큰 재발급 시점 명확화
        // 인증 이벤트 경계가 분명하게
        // await userManager.UpdateSecurityStampAsync(user);

        // AccessToken
        var token = await tokenService.GenerateAccessTokenAsync(user);
        var refreshtToken = tokenService.GenerateRefreshToken();
        user.RefreshToken = refreshtToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        if (dto.RememberDevice)
        {
            user.TrustedDeviceKey = dto.DeviceFingerprint;
            user.TrustedDeviceExpiry = DateTime.UtcNow.AddDays(30);
        }

        await userManager.UpdateAsync(user);
        var isEmailConfirmed = await userManager.IsEmailConfirmedAsync(user);
        var warningMessage = !isEmailConfirmed
            ? "이메일인증이 완료되지 않았습니다. 일부기능에 제한이 있을 수 있습니다."
            : string.Empty;

        // ✅ 실제 사용자의 2FA 활성화 상태 확인
        var twoFactorEnabled = await userManager.GetTwoFactorEnabledAsync(user);

        var result = new TwoFactorSchema.Identity
        {
            IsSuccess = true,
            Token = token,
            RefreshToken = refreshtToken,
            Message = $"로그인에 성공하였습니다. \n{warningMessage}",
            RefreshTokenExpiry = user.RefreshTokenExpiryTime,
            EmailConfirmed = isEmailConfirmed,
            ShowEmailWarning = !isEmailConfirmed,
            RequiresTwoFactor = false,
            TwoFactorEnabled = twoFactorEnabled
        };
        return Result<TwoFactorSchema.Identity>.Success(result, result.Message);
    }

    /// <summary>
    /// 복구코드를 사용한 로그인
    /// </summary>
    public async Task<Result<TwoFactorSchema.Identity>> LoginWithRecoveryCodeAsync(
        [FromBody] TwoFactorSchema.RecoveryCodeLogin dto)
    {
        var user = await userManager.FindByEmailAsync(dto.Email);

        if (user is null)
            return Result<TwoFactorSchema.Identity>.Failure(RsCode.Unauthorized, "사용자를 찾을 수 없습니다.");

        // 2FA 활성화 확인
        if (!user.TwoFactorEnabled)
            return Result<TwoFactorSchema.Identity>.Failure(RsCode.BadRequest, "2단계 인증이 활성화되어 있지 않습니다.");

        // ✅ 계정 잠금 확인
        if (await userManager.IsLockedOutAsync(user))
            return Result<TwoFactorSchema.Identity>.Failure(RsCode.Unauthorized, "계정이 잠겼습니다. 잠시 후 다시 시도하여 주세요");

        // 복구 코드 검증 및 사용
        var result = await userManager.RedeemTwoFactorRecoveryCodeAsync(user, dto.RecoveryCode!);

        if (!result.Succeeded)
        {
            // 실패횟수 증가
            await userManager.AccessFailedAsync(user);
            var failMessage = new TwoFactorSchema.Identity
            {
                IsSuccess = false,
                Message = "잘못된 복구 코드입니다. 복구 코드는 한 번만 사용할 수 있습니다.",
                Token = string.Empty,
                RefreshToken = string.Empty
            };
        }

        // 성공 시 실패 횟수 리셋
        await userManager.ResetAccessFailedCountAsync(user);

        // 보안 스탬프 업데이트
        await userManager.UpdateSecurityStampAsync(user);

        var token = await tokenService.GenerateAccessTokenAsync(user);
        var refreshToken = tokenService.GenerateRefreshToken();

        await userManager.UpdateAsync(user);

        var isEmailConfirmed = await userManager.IsEmailConfirmedAsync(user);
        var warningMessage = !isEmailConfirmed
            ? "이메일인증이 완료되지 않았습니다. 일부기능에 제한이 있을 수 있습니다."
            : string.Empty;

        var twoFactorEnabled = await userManager.GetTwoFactorEnabledAsync(user);

        // 남은 복구 코드 개수 확인
        var remainingCodes = await userManager.CountRecoveryCodesAsync(user);

        var identity = new TwoFactorSchema.Identity
        {
            IsSuccess = true,
            Token = token,
            RefreshToken = refreshToken,
            Message = $"복구 코드로 로그인에 성공하였습니다. {warningMessage} 남은 복구 코드: {remainingCodes}개",
            RefreshTokenExpiry = user.RefreshTokenExpiryTime,
            EmailConfirmed = isEmailConfirmed,
            ShowEmailWarning = !isEmailConfirmed,
            RequiresTwoFactor = false,
            TwoFactorEnabled = twoFactorEnabled
        };
        return Result<TwoFactorSchema.Identity>.Success(identity, identity.Message);
    }

    /// <summary>
    /// 2단계 인증 비활성화
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    public async Task<Result> DisableTwoFactorAsync(ApplicationUser user)
    {
        var result = await userManager.SetTwoFactorEnabledAsync(user, false);
        if (!result.Succeeded)
            return Result.Failure(RsCode.BadRequest, "2단계 인증 비활성화에 실패하였습니다.");

        await userManager.ResetAuthenticatorKeyAsync(user);

        return Result.Success("2단계 인증이 비활성화되었습니다.");
    }

    // ====== Helper 메서드들 ======

    private string GenerateQrCodeUri(string email, string unformattedKey)
    {
        const string authenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";
        return string.Format(
            authenticatorUriFormat,
            Uri.EscapeDataString("BuddhamApp"),
            Uri.EscapeDataString(email),
            unformattedKey
        );
    }

    private string GenerateQrCode(string text)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        var qrCodeImage = qrCode.GetGraphic(20);
        return Convert.ToBase64String(qrCodeImage);
    }

    private string FormatKey(string unformattedKey)
    {
        var result = new StringBuilder();
        int currentPosition = 0;
        while (currentPosition + 4 < unformattedKey.Length)
        {
            result.Append(unformattedKey.AsSpan(currentPosition, 4)).Append(' ');
            currentPosition += 4;
        }

        if (currentPosition < unformattedKey.Length)
            result.Append(unformattedKey.AsSpan(currentPosition));
        return result.ToString().ToLowerInvariant();
    }
    // public async Task<Result<TwoFactorSchema.Setup>> EnableTwoFactorAsync(ApplicationUser user)
    // {
    //     throw new NotImplementedException();
    // }
    //
    // public async Task<Result<TwoFactorSchema.VerifyResponse>> VerifyTwoFactorAsync(TwoFactorSchema.Verify dto, ApplicationUser user)
    // {
    //     throw new NotImplementedException();
    // }
    //
    // public async Task<Result<TwoFactorSchema.Identity>> LoginWithTwoFactorAsync(TwoFactorSchema.Login dto)
    // {
    //     throw new NotImplementedException();
    // }
    //
    // public async Task<Result<TwoFactorSchema.Identity>> LoginWithRecoveryCodeAsync(TwoFactorSchema.RecoveryCodeLogin dto)
    // {
    //     throw new NotImplementedException();
    // }
    //
    // public async Task<Result> DisableTwoFactorAsync(ApplicationUser user)
    // {
    //     throw new NotImplementedException();
    // }
}
