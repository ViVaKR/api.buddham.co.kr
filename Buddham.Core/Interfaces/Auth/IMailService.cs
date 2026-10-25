namespace Buddham.Core.Interfaces.Auth;

public interface IMailService
{
    Task<bool> SendMailAsync(string toEmail, string subject, string body);
}
