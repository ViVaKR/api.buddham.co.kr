namespace Buddham.Shared.Constants;

public static class RedisKeys
{
    // 1. 네임스페이스 (Prefix) 정의
    private const string Root = "Buddham";
    private const string Admin = "Admin";
    private const string Auth = "Auth";
    private const string Scripture = "Scripture";

    // --> User
    public static string UserList => $"{Root}:{Auth}:User:List";
    public static string UserDetail(Guid id) => $"{Root}:{Auth}:User:Detail:{id}";
    public static string TotaAccount => $"{Root}:{Auth}:TotalAccount";

    // --> Scripture
    public static string ScriptureMajors => $"{Root}:{Scripture}:Major:List";
    public static string ScriptureDetail(int id) => $"{Root}:{Scripture}:Detail:{id}";
}
