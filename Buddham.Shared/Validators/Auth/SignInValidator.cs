using Buddham.Core.Schemas;
using FluentValidation;

namespace Buddham.Shared.Validators.Auth;

public class SignInValidator : AbstractValidator<AuthSchema.SignIn>
{
    public SignInValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}
