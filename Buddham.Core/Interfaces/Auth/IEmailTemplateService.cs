namespace Buddham.Core.Interfaces.Auth;

public interface IEmailTemplateService
{
    string GetWelcomeEmailHtml(string name, string pseudonym, string link);
    string SignUpMessage(string fullName, string pseudonym, string confirmationLink); 
}
