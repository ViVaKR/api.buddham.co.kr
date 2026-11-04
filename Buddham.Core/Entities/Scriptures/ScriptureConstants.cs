namespace Buddham.Core.Entities.Scriptures;

/// <summary>
/// 임베딩 관련 상수• 모델(bge-m3)을 바꾸면 차원과 함께 갱신함.
/// </summary>
public static class ScriptureConstants
{
    public const int EmbeddingDimensions = 1024;
    public const string EmbeddingColumnType = "vector(1024)";
}
