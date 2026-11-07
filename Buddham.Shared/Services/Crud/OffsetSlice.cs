namespace Buddham.Shared.Services.Crud;

/// <summary>Offset 페이지 캐시용 슬라이스 (직렬화 가능한 단순 레코드).</summary>
public sealed record OffsetSlice<T>(List<T> Items, int TotalCount);
