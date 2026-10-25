using Buddham.Core.Schemas;
using FluentValidation;

namespace Buddham.Shared.Validators.Auth;

public class SignUpValidator :  AbstractValidator<AuthSchema.SignUp>
{
    public SignUpValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
        RuleFor(x=> x.Pseudonym).NotEmpty().MinimumLength(2);
        RuleFor(x=> x.FullName).NotEmpty().MinimumLength(2);
    }
}
