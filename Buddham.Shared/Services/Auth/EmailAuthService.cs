using System.Text;
using Buddham.Core.Common;
using Buddham.Core.Common.Results;
using Buddham.Core.Entities.Auth;
using Buddham.Core.Interfaces.Auth;
using Buddham.Core.Schemas;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

namespace Buddham.Shared.Services.Auth;

public class EmailAuthService(
    IMailService mailService,
    UserManager<ApplicationUser> userManager
) : IEmailAuthService
{
    /// <summary>
    /// 비밀번호 분실
    /// </summary>
    public async Task<Result<EmailSchema.Identity>> ForgotPasswordAsync(EmailSchema.ForgotPassword dto)
    {
        var user = await userManager.FindByEmailAsync(dto.Email);

        // 1. 유저가 있고 + 이메일 인증까지 완료된 "성공 가도"일 때
        if (user != null && await userManager.IsEmailConfirmedAsync(user))
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var b = new UriBuilder(dto.ReplyUrl.Trim())
            {
                Query = $"email={Uri.EscapeDataString(user.Email!)}&token={encodedToken}"
            };
            var resetLink = b.ToString();
            var message = BuildVerifyEmailHtml(resetLink, user.FullName);
            await mailService.SendMailAsync(user.Email!, "비밀번호 재설정 안내", message);
            var successPayload = EmailSchema.Identity.Success($"{user.Email} 로 재설정 링크를 전송하였습니다.", user.Pseudonym);
            return Result<EmailSchema.Identity>.Success(successPayload, "이메일이 발송되었습니다.");
        }

        var fakePayload = EmailSchema.Identity.Success($"{dto.Email} 로 비밀번호 재설정 링크를 전송하였습니다.",
            user?.Pseudonym ?? "사용자");
        return Result<EmailSchema.Identity>.Success(fakePayload, "비밀번호 재설정 링크가 발송되었습니다. 이메일을 확인해주세요.");
    }

    public async Task<Result<EmailSchema.Identity>> ForgotPassword2Async(EmailSchema.ForgotPassword dto)
    {
        var user = await userManager.FindByEmailAsync(dto.Email);
        if (user != null && await userManager.IsEmailConfirmedAsync(user))
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            // 1. Dictionary로 깔끔하게 쿼리 매개변수 생성 (수동 인코딩 탈출!)
            var queryParams = new Dictionary<string, string?>
            {
                { "email", user.Email },
                { "token", encodedToken }
            };
            // 2. QueryHelpers가 기존 쿼리는 보존하고 새 쿼리는 인코딩해서 붙여줌!
            var resetLink = QueryHelpers.AddQueryString(dto.ReplyUrl.Trim(), queryParams);
            var message = BuildVerifyEmailHtml(resetLink, user.FullName);
            await mailService.SendMailAsync(user.Email!, "비밀번호 재설정 안내", message);

            var successPayload = EmailSchema.Identity.Success($"{user.Email} 로 재설정 링크를 전송하였습니다.", user.Pseudonym);
            return Result<EmailSchema.Identity>.Success(successPayload, "이메일이 발송되었습니다.");
        }

        var fakePayload = EmailSchema.Identity.Success($"{dto.Email} 로 비밀번호 재설정 링크를 전송하였습니다.",
            user?.Pseudonym ?? "사용자");
        return Result<EmailSchema.Identity>.Success(fakePayload, "비밀번호 재설정 링크가 발송되었습니다. 이메일을 확인해주세요.");
    }

    /// <summary>
    /// 이메일 인증요청 메일전송
    /// </summary>
    public async Task<Result> VerifySendEmailAsync(EmailSchema.VerifyEmailRequest dto)
    {
        // 1. 사용자 확인
        var user = await userManager.FindByEmailAsync(dto.Email);

        // [자비 포인트] 사용자가 없거나 이미 인증되었어도 외부에는 동일하게 '성공' 응답을 줌 (보안)
        if (user is null || user.EmailConfirmed)
        {
            // 로그에는 남기되, 클라이언트에게는 발송했다고 거짓말(?)하는 자비로움
            return Result.Success("이메일 인증 메일이 발송되었습니다. (Security Masking)");
        }

        // 2. 토큰 생성 및 인코딩
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        // 3. 링크 생성 (문자열 보간 가독성 향상)
        var confirmationLink = $"{dto.ReplyUrl}?email={Uri.EscapeDataString(user.Email!)}&token={encodedToken}";

        // 4. 메일 발송
        var mailBody = BuildConfirmationEmailHtml(confirmationLink);
        await mailService.SendMailAsync(user.Email!, "[Buddham] 이메일 인증을 완료해주세요", mailBody);
        return Result.Success("이메일 인증 메일이 발송되었습니다. 이메일을 확인해주세요.");
    }

    /// <summary>
    /// 이메일 인증요청 완료하기
    /// </summary>
    public async Task<Result> ConfirmEmailAsync(EmailSchema.ConfirmEmail dto)
    {
        // 1. 사용자 존재 여부 확인
        var user = await userManager.FindByEmailAsync(dto.Email);
        if (user is null)
        {
            // 보안상 '사용자 없음'을 구체적으로 알리지 않고 실패 처리
            return Result.Failure(RsCode.Forbidden, "인증 정보를 확인할 수 없습니다.");
        }

        // 2. 이미 인증된 경우 (중복 요청 자비롭게 처리)
        if (user.EmailConfirmed)
        {
            return Result.Success("이미 인증이 완료된 사용자입니다. 로그인을 진행해 주세요.");
        }

        try
        {
            // 3. 토큰 디코딩 (안전하게 처리)
            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(dto.Token));

            // 4. Identity 시스템에 인증 확정 요청
            var result = await userManager.ConfirmEmailAsync(user, decodedToken);

            if (!result.Succeeded)
            {
                // 로그에는 상세한 에러를 남기되, 사용자에게는 정제된 메시지 전달
                // 예: 토큰 만료, 잘못된 토큰 등
                return Result.Failure(RsCode.BadRequest, "인증 토큰이 유효하지 않거나 만료되었습니다.");
            }

            // 5. 성공! 진리의 문이 열림
            return Result.Success("이메일 인증이 성공적으로 완료되었습니다.");
        }
        catch (Exception ex)
        {
            // 디코딩 실패나 예상치 못한 마귀 침입 시
            // 로깅: _logger.LogError(ex, "Email verification failed");
            return Result.Failure(RsCode.Error, $"인증 처리 중 오류가 발생했습니다. 다시 시도해 주세요.\n{ex.Message}");
        }
    }

    public async Task<bool> SendTestEmailAsync(string email)
    {
        bool result =
            await mailService.SendMailAsync(email, "테스트 이메일입니다.", "<h1>이것은 테스트 이메일입니다.</h1><p>이메일 서비스를 확인합니다.</p>");
        return result;
    }

    /// <summary>
    /// 이메일 인증 요청 시 발송되는 메일의 HTML 템플릿을 생성하는 메서드입니다.
    /// </summary>
    private static string BuildConfirmationEmailHtml(string link) =>
        $"""
         <div style="font-family:sans-serif;max-width:560px;margin:0 auto;border-radius:12px;overflow:hidden;border:1px solid #e8e8e4;">

         <div style="background:#2C2C2A;padding:2rem;text-align:center;">
          <div style="font-size:24px;color:#FAC775;letter-spacing:0.15em;font-weight:500;">🪷 BUDDHAM</div>
          <div style="font-size:11px;color:#888780;margin-top:6px;letter-spacing:0.2em;">buddham.co.kr</div>
         </div>

         <div style="padding:2.5rem 2rem;text-align:center;background:#ffffff;">
          <div style="width:56px;height:56px;border-radius:50%;background:#FAEEDA;display:inline-flex;align-items:center;justify-content:center;margin-bottom:1.5rem;">
            <span style="font-size:24px;">✉️</span>
          </div>
          <p style="font-size:18px;font-weight:500;color:#2C2C2A;margin:0 0 1.5rem;">이메일 인증</p>

          <div style="border-left:3px solid #FAC775;padding-left:1rem;margin:1.5rem 0;text-align:left;">
            <p style="font-size:14px;color:#5F5E5A;margin:0;line-height:1.8;">
              부처님의 자비 아래,<br>아래 버튼을 클릭하여 인증을 완료하십시오.
            </p>
          </div>

          <div style="margin:2rem 0;">
            <a href="{link}" style="display:inline-block;background:#2C2C2A;color:#FAC775;text-decoration:none;padding:14px 40px;border-radius:8px;font-size:15px;font-weight:500;letter-spacing:0.05em;">
              이메일 확인하기
            </a>
          </div>

          <div style="background:#f9f9f7;border-radius:8px;padding:1rem 1.25rem;margin-bottom:1.5rem;text-align:left;">
            <p style="font-size:12px;color:#888780;margin:0 0 6px;">링크가 작동하지 않나요?</p>
            <a href="{link}" style="font-size:11px;color:#BA7517;word-break:break-all;text-decoration:none;">{link}</a>
          </div>

          <div style="border-top:1px solid #e8e8e4;padding-top:1.5rem;">
            <p style="font-size:12px;color:#B4B2A9;margin:0;line-height:1.8;text-align:center;">
              본 메일은 발신 전용입니다. 직접 요청하지 않으셨다면 무시하셔도 됩니다.<br>
              © 2026 Buddham · 불법(佛法)의 웹
            </p>
          </div>
         </div>

         </div>
         """;

    private static string BuildVerifyEmailHtml(string resetLink, string fullName) =>
        $"""
         <div style="background:#f5f5f0;padding:2rem;font-family:sans-serif;">
         <div style="max-width:560px;margin:0 auto;background:#ffffff;border-radius:12px;overflow:hidden;border:1px solid #e8e8e4;">

         <div style="background:#2C2C2A;padding:2rem;text-align:center;">
         <div style="font-size:24px;color:#FAC775;letter-spacing:0.15em;font-weight:500;">🪷 BUDDHAM</div>
         <div style="font-size:11px;color:#888780;margin-top:6px;letter-spacing:0.2em;">buddham.co.kr</div>
         </div>

         <div style="padding:2.5rem 2rem;">
         <p style="font-size:14px;color:#888780;margin:0 0 4px;">안녕하세요,</p>
         <p style="font-size:20px;font-weight:500;color:#2C2C2A;margin:0 0 1.5rem;">{fullName} 님</p>

         <div style="border-left:3px solid #FAC775;padding-left:1rem;margin-bottom:2rem;">
         <p style="font-size:14px;color:#5F5E5A;margin:0;line-height:1.8;">
         비밀번호 재설정 요청이 접수되었습니다.<br>
         아래 버튼을 통해 새로운 비밀번호를 설정하실 수 있습니다.
         </p>
         </div>

         <div style="text-align:center;margin:2rem 0;">
         <a href="{resetLink}"
         style="display:inline-block;background:#2C2C2A;color:#FAC775;text-decoration:none;
         padding:14px 40px;border-radius:8px;font-size:15px;font-weight:500;letter-spacing:0.05em;">
         비밀번호 재설정
         </a>
         </div>

         <div style="background:#f9f9f7;border-radius:8px;padding:1rem 1.25rem;margin-bottom:2rem;">
         <p style="font-size:13px;color:#5F5E5A;margin:0;">
         ⏱ 이 링크는 발송 후 <strong style="color:#2C2C2A;">1시간</strong> 동안만 유효합니다.
         </p>
         </div>

         <div style="border-top:1px solid #e8e8e4;padding-top:1.5rem;text-align:center;">
         <p style="font-size:12px;color:#B4B2A9;margin:0;line-height:1.8;">
         본 메일은 발신 전용입니다. 직접 요청하지 않으셨다면 무시하셔도 됩니다.<br>
         © 2026 Buddham · 불법(佛法)의 웹
         </p>
         </div>
         </div>

         </div>
         </div>
         """;
    // public async Task<Result> VerifySendEmailAsync(EmailSchema.VerifyEmailRequest dto)
    // {
    //     throw new NotImplementedException();
    // }
    //
    // public async Task<Result> ConfirmEmailAsync(EmailSchema.ConfirmEmail dto)
    // {
    //     throw new NotImplementedException();
    // }
    //
    // public async Task<Result<EmailSchema.Identity>> ForgotPasswordAsync(EmailSchema.ForgotPassword dto)
    // {
    //     throw new NotImplementedException();
    // }
    //
    // public async Task<bool> SendTestEmailAsync(string email)
    // {
    //     throw new NotImplementedException();
    // }
}
