namespace Buddham.Shared.Constants;

public static class UploadSize
{
    public const long KB = 1024;
    public const long MB = 1024 * KB;
    public const long GB = 1024 * MB;

    public const long Avatar = 2 * MB; // 아바타
    public const long Image = 10 * MB; // 일반 이미지
    public const long File = 50 * MB; // 일반 파일
    public const long Audio = 100 * MB; // 오디오
    public const long Document = 20 * MB; // 문서
    public const long Video = 1 * GB; // 영상 (관리자)
}
