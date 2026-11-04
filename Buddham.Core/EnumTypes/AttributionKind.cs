namespace Buddham.Core.EnumTypes;

/// <summary>
/// 전숭상의 귀속
/// 역사적 사실 주장이 아니라 전승 기록
/// 학술적 평가는 AttributionNote에 둠
/// </summary>
public enum AttributionKind
{
    Unknown,
    Buddha,
    // 제작의 설법을 붓다가 인가
    ApprovedDisciple,
    Disciple,
    Other
}
