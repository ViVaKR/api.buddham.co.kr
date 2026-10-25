namespace Buddham.Core.Schemas;

public class EmailSchema
{
    public record ForgotPassword(string Email, string ReplyUrl);
    public record VerifyEmailRequest(string Email, string ReplyUrl);
    public record ConfirmEmail(string Email, string Token);
    public record SendTestMail(string ToMail);
    public record Identity
    {
        public string? Message { get; set; }
        public string? Pseudonym { get; set; }

        public static Identity Success( string message, string pseudonym ) => new() 
        {
            Message = message,
            Pseudonym = pseudonym
        };

        public static Identity Fail( string message ) => new() { Message = message };
    } 
}
