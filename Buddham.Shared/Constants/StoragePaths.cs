namespace Buddham.Shared.Constants;

public static class StoragePaths
{
    // 예: /var/www/buddham/storage
    public static string Avatars(Guid userId, string absoluteRoot)
        => Path.Combine(absoluteRoot, "Avatars", userId.ToString());

    public static string Images(string absoluteRoot)
        => Path.Combine(absoluteRoot, "Images");

    public static string Videos(string absoluteRoot)
        => Path.Combine(absoluteRoot, "Videos");

    // 웹 URL용은 그대로 유지 (절대 경로와 웹 경로는 철저히 분리!)
    public static class Web
    {
        public static string AvatarDir(Guid userId) => $"/Avatars/{userId}";
        public static string Avatar(Guid userId, string fileName) => $"/Avatars/{userId}/{fileName}";
        public static string DefaultAvatar => "/Avatars/default/buddha.png";
    }
}
