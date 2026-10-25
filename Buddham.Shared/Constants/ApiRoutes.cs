namespace Buddham.Shared.Constants;

public static class ApiRoutes
{
    public static class Auth
    {
        public const string SignIn = "/SignIn";
        public const string SignOut = "/SignOut";
        public const string SignUp = "/SignUp";
    }

    public static class Files
    {
        public const string Avatars = "/Avatars";
        public const string Thumbnails = "/Thumbnails";
        public const string Images = "/Images";
        public const string Videos = "/videos";
    }

    public static class Statics
    {
        public const string Healthz = "/healthz";
        public const string Root = "/app/Storage";
    }
}