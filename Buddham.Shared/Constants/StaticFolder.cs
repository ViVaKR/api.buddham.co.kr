namespace Buddham.Shared.Constants;

public static class StaticFolder
{
    private const string Root = ApiRoutes.Statics.Root;

    public static Dictionary<string, string> GetStaticWebFolders()
    {
        return new Dictionary<string, string>
        {
            { "/Images", Path.Combine(Root, "Images") },
            { "/Avatars", Path.Combine(Root, "Avatars") },
            { "/Files", Path.Combine(Root, "Files") },
            { "/Audio", Path.Combine(Root, "Audio") },
            { "/Videos", Path.Combine(Root, "Videos") },
            { "/Documents", Path.Combine(Root, "Documents") },
            { "/Attachments", Path.Combine(Root, "Attachments") }
        };
    }
}
