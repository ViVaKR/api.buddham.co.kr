namespace Buddham.Core.Interfaces.Public;

/// <summary>
/// 코드 + 이름 3종(한글•원어•로마자)을 공유한는 엔터티
/// Configration 중복 제거용
/// </summary>
public interface ICodedName
{
    string Code { get; set; }
    string NameKo { get; set; }
    string? NameOriginal { get; set; }
    string? NameRoman { get; set; }
}
