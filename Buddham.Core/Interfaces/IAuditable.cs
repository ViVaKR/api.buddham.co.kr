namespace Buddham.Core.Interfaces;

/// <summary>
/// 행적, 이력, 연혁, 행적
/// CreatedAt - 탄생의 흔적
/// UpdatedAt - 변화의 흔적
/// DeletedAt - 입멸의 흔적
/// </summary>
public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; } 
}
