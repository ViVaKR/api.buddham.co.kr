using Buddham.Core.EnumTypes;

namespace Buddham.Shared.Extensions.Tangwha;

public static class BuddhistCategoryExtensions
{
    /// <summary>
    /// 사용법: buddhistCategory.ToKorean();
    /// </summary>
    /// <param name="category"></param>
    /// <returns></returns>
    public static string ToKorean(this BuddhistCategory category) => category switch
    {
        BuddhistCategory.Bulbosal => "불보살",
        BuddhistCategory.Nahan => "나한",
        BuddhistCategory.Sinjung => "신중",
        BuddhistCategory.Sansin => "산신",
        BuddhistCategory.Etc => "기타",
        _ => "미지정"
    };
}
