using Buddham.Core.Common.Primitives;

namespace Buddham.Core.Entities.Public;

public class Tangwha : EntityBase
{
    public string Title { get; set; } = default!;

    // 카테고리 : 불보살|나한|신중|산신|기타
    public string Category { get; set; } = default!;
    public int MyProperty { get; set; }
}
