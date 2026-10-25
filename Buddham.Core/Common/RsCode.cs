namespace Buddham.Core.Common;

/// <summary>
/// 응답 상태 코드
/// </summary>
public enum RsCode
{
    /// <summary>
    /// 조회 성공 200
    /// 가장 일반적인 OK
    /// </summary>
    Ok = 200,

    /// <summary>
    /// 생성 완료 201
    /// 주로 POST 결과
    /// </summary>
    Created = 201,

    /// <summary>
    ///  승인대기/작업예약 (202)
    ///  당장 결과는 안 나오지만 접수는 됐을때, 비동기 작업용
    /// </summary>
    Accepted = 202,

    /// <summary>
    /// 내용 없음 204
    /// 수정/삭제 완료
    /// </summary>
    NoContent = 204,

    /// <summary>
    /// 잘못된 요청 400
    /// URL 입력 실수 및 쿠키/캐시 문제등
    /// 사용자 쪽세어 해결 가능 함
    /// URL 정확히 입력하거나 캐시 쿠키를 삭제 하여 해결
    /// </summary>
    BadRequest = 400,

    /// <summary> 인증 필요 (401) </summary>
    Unauthorized = 401,

    /// <summary>
    /// 권한 부족 403
    /// </summary>
    Forbidden = 403,

    /// <summary>
    /// 데이터 없음 404
    /// </summary>
    NotFound = 404,

    /// <summary>
    /// 중복 에러 409
    /// 이미 있는 데이터 등
    /// </summary>
    Conflict = 409,

    // 3. 422 추가
    UnprocessableEntity = 422,
    
    /// <summary> 서버오류 (500) </summary>
    Error = 500
}
