using Buddham.Core.Entities.Auth;

namespace Buddham.Core.Interfaces.Auth;

public interface IEmailService
{
    Task<bool> SendEmailAsync(string email, string subject, string body);
    Task SendPasswordResetEmailAsync(ApplicationUser user, string token, string replyUrl);
    Task<bool> SendTestEmailAsync(string email); 
}
