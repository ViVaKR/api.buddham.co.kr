namespace Buddham.Shared.Constants;

public class AuditActions
{
    // Auth
    public const string SignIn = "SignIn";
    public const string SignOut = "SignOut";
    public const string RefreshToken = "RefreshToken";

    // Account
    public const string SignUp = "SignUp";
    public const string UpdateUser = "UpdateUser";
    public const string DeleteUser = "DeleteUser";
    public const string ChangePassword = "ChangePassword";
    public const string ResetPassword = "ResetPassword";

    // Email
    public const string ConfirmEmail = "ConfirmEmail";
    public const string ForgotPassword = "ForgotPassword";
}
