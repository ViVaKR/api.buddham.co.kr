using Buddham.Core.Common.Results;
using Buddham.Core.Schemas;

namespace Buddham.Core.Interfaces.Auth;

public interface IEmailAuthService
{
    Task<Result> VerifySendEmailAsync(EmailSchema.VerifyEmailRequest dto);

    Task<Result> ConfirmEmailAsync(EmailSchema.ConfirmEmail dto);

    Task<Result<EmailSchema.Identity>> ForgotPasswordAsync(EmailSchema.ForgotPassword dto);

    Task<bool> SendTestEmailAsync(string email);
}
