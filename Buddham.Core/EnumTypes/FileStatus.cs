namespace Buddham.Core.EnumTypes;

public enum FileStatus
{
    Pending, // 업로드 중
    Processing = 1, // Video 처리중
    Completed, // 정상 완료
    Failed // 실패 (롤백 대상) 
}