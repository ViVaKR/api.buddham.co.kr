using Buddham.Core.Common.Results;
using Buddham.Core.Entities.Auth;
using Buddham.Core.Schemas;

namespace Buddham.Core.Interfaces.Auth;

public interface ITwoFactorService
{
    Task<Result<TwoFactorSchema.Setup>> EnableTwoFactorAsync(ApplicationUser user);

    Task<Result<TwoFactorSchema.VerifyResponse>> VerifyTwoFactorAsync(TwoFactorSchema.Verify dto,
        ApplicationUser user);

    Task<Result<TwoFactorSchema.Identity>> LoginWithTwoFactorAsync(TwoFactorSchema.Login dto);
    Task<Result<TwoFactorSchema.Identity>> LoginWithRecoveryCodeAsync(TwoFactorSchema.RecoveryCodeLogin dto);
    Task<Result> DisableTwoFactorAsync(ApplicationUser user);
}
