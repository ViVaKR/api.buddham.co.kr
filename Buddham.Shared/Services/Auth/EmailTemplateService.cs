using Buddham.Core.Interfaces.Auth;

namespace Buddham.Shared.Services.Auth;

public class EmailTemplateService : IEmailTemplateService
{
    public string GetWelcomeEmailHtml(string name, string pseudonym, string link)
    {
        return $"""
                <div style="font-family: sans-serif; max-width: 600px; margin: 2em auto; padding: 2em; border: 1px solid #ddd; border-radius: 12px;">
                    <h2 style="color: #1976d2;">환영합니다, {name} ({pseudonym}) 님! 🎉</h2>
                    <p>회원가입을 진심으로 축하드립니다.</p>
                    <p style="margin: 2em 0; text-align: center;">
                        <a href="{link}" style="background:#1976d2; color:white; padding:1em 2.5em; text-decoration:none; border-radius:8px; font-weight:bold; display:inline-block;">지금 바로 이메일 인증하기 ✓</a>
                    </p>
                    </div>
                """;
    }

    public string SignUpMessage(string fullName, string pseudonym, string confirmationLink)
    {
        return $"""
                    <div style="font-family: sans-serif; max-width: 600px; margin: 2em auto; padding: 2em; border: 1px solid #ddd; border-radius: 12px;">
                        <h2 style="color: #1976d2;">환영합니다, {fullName} ({pseudonym}) 님! 🎉</h2>
                        <p>회원가입을 진심으로 축하드립니다.</p>

                        <div style="background: #fff3cd; border-left: 4px solid #ffc107; padding: 1em; margin: 1.5em 0;">
                            <h3 style="margin-top: 0; color: #856404;">⚠️ 중요: 이메일 인증 필수</h3>
                            <p style="margin: 0.5em 0;"><strong>이메일 인증을 완료하지 않으면:</strong></p>
                            <ul style="margin: 0.5em 0; padding-left: 1.5em;">
                                <li>❌ 비밀번호 분실 시 <strong>복구 불가능</strong></li>
                                <li>⚠️ 일부 기능 사용 제한</li>
                            </ul>
                        </div>

                        <p style="margin: 2em 0; text-align: center;">
                            <a href="{confirmationLink}"
                                style="background:#1976d2; color:white; padding:1em 2.5em; text-decoration:none; border-radius:8px; font-weight:bold; display:inline-block;">
                                지금 바로 이메일 인증하기 ✓
                            </a>
                        </p>

                        <p style="font-size: 0.9em;">버튼이 작동하지 않으면 아래 링크를 복사해서 사용해주세요:</p>
                        <p style="background: #f5f5f5; padding: 1em; word-break: break-all; font-size: 0.85em; border-radius: 4px;">
                            {confirmationLink}
                        </p>

                        <hr style="margin: 2em 0; border: none; border-top: 1px solid #ddd;"/>

                        <p style="color: #666; font-size: 0.9em; margin: 0.5em 0;">
                            📅 이 링크는 <strong>24시간 후</strong> 만료됩니다.
                        </p>
                        <p style="color: #666; font-size: 0.9em; margin: 0.5em 0;">
                            💡 인증 후 모든 기능을 안전하게 이용하실 수 있습니다.
                        </p>

                        <p style="margin-top: 2em; color: #666;">
                            긴 여정 속에서 좋은 인연으로 남겠습니다. 감사합니다.<br/>
                            <strong>사이트 운영자 홍법 드림</strong>
                        </p>
                </div>
                """;
    }
}
