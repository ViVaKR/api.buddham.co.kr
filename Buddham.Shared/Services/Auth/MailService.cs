using Buddham.Core.Common.Options;
using MimeKit;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using IMailService = Buddham.Core.Interfaces.Auth.IMailService;

namespace Buddham.Shared.Services.Auth;

public class MailService(IOptions<EmailConfigOptions> options) : IMailService
{
    private readonly EmailConfigOptions _config = options.Value;

    public async Task<bool> SendMailAsync(string toEmail, string subject, string body)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_config.FromName, _config.From));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        var bodyBuilder = new BodyBuilder { HtmlBody = body };
        message.Body = bodyBuilder.ToMessageBody();
        using var client = new SmtpClient();
        await client.ConnectAsync(_config.Host, _config.Port, MailKit.Security.SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(_config.From, _config.Password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
        return true;
    }
}
