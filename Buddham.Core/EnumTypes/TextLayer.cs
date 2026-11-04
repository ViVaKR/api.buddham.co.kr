namespace Buddham.Core.EnumTypes;

/// <summary>
/// 원전/주석/복주 구분
/// 주석서는 부처님 말씀이 아니므로 반드시 구분함.
/// </summary>
public enum TextLayer
{
    Root,
    Commentary,
    SubCommentary,
    Other
}
