using Buddham.Core.Common;
using Buddham.Shared.Constants;
using Buddham.Shared.Middlewares;

namespace Buddham.Shared.Services.Crud;

// ══════════════════════════════════════════════════════════
// 1) 페이징/검색 입력 검증 (BadRequest 예외)
// ══════════════════════════════════════════════════════════

public static class Paging
{
    public static void EnsureSize(int size)
    {
        if (size is < 1 or > AppInfrastructure.MaxPageSize)
            throw new BuddhamException(RsCode.BadRequest,
                $"페이지 크기는 1~{AppInfrastructure.MaxPageSize} 사이여야 합니다.");
    }

    public static void EnsureOffsetPage(int pageNumber, int pageSize)
    {
        if (pageNumber < 1)
            throw new BuddhamException(RsCode.BadRequest, "페이지 번호는 1 이상이어야 합니다.");

        EnsureSize(pageSize);

        if ((long)(pageNumber - 1) * pageSize > AppInfrastructure.MaxOffsetDepth)
            throw new BuddhamException(RsCode.BadRequest, "조회 범위를 벗어난 페이지입니다. 검색 조건을 좁혀 주세요.");
    }

    public static void EnsureKeyword(string keyword)
    {
        if (keyword.Length > AppInfrastructure.MaxKeywordLength)
            throw new BuddhamException(RsCode.BadRequest,
                $"검색어는 {AppInfrastructure.MaxKeywordLength}자 이하여야 합니다.");
    }

    /// <summary>NaN 도 이 조건에서 걸러진다.</summary>
    public static void EnsureThreshold(float threshold)
    {
        if (!(threshold is >= 0f and <= 1f))
            throw new BuddhamException(RsCode.BadRequest, "Threshold 는 0 ~ 1 사이어야 합니다.");
    }
}
