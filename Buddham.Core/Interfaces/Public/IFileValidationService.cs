namespace Buddham.Core.Interfaces.Public;

public interface IFileValidationService
{
    bool IsValidFile(string fileName, string contentType);
    bool IsValidAvatarFile(string fileName, string contentType);
    string GetInvalidReason(string fileName, string contentType);
}