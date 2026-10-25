using Buddham.Core.Entities.Auth;
using Buddham.Core.Interfaces.Auth;

namespace Buddham.Shared.Services.Auth;

public class EmailService : IEmailService
{
    public async Task<bool> SendEmailAsync(string email, string subject, string body)
    {
        throw new NotImplementedException();
    }

    public async Task SendPasswordResetEmailAsync(ApplicationUser user, string token, string replyUrl)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> SendTestEmailAsync(string email)
    {
        throw new NotImplementedException();
    }
}