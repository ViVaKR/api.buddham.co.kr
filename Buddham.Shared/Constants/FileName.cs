namespace Buddham.Shared.Constants;

public static class FileName
{
    public static string Generate()
    {
        return $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}";
    }
}