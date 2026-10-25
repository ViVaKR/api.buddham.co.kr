using Buddham.Core.Entities.Public;

namespace Buddham.Core.Common.Results;

public record Result : ResultBase
{
    // ══════════════════════════════════════════
    // ✅ 성공 시리즈
    // ══════════════════════════════════════════

    public static Result Success(string msg = "요청이 성공적으로 처리되었습니다.")
        => new() { IsSuccess = true, Message = msg, Code = RsCode.Ok };

    public static Result Created(string msg = "새로운 리소스가 생성되었습니다.")
        => new() { IsSuccess = true, Message = msg, Code = RsCode.Created };

    public static Result Updated(string msg = "정보가 성공적으로 수정되었습니다.")
        => new() { IsSuccess = true, Message = msg, Code = RsCode.Ok };

    public static Result NoContent(string msg = "요청이 성공적으로 처리되었으며 반환할 데이터가 없습니다.")
        => new() { IsSuccess = true, Message = msg, Code = RsCode.NoContent };

    public static Result Accepted(string msg = "요청이 접수되어 처리 중입니다.")
        => new() { IsSuccess = true, Message = msg, Code = RsCode.Accepted };

    // ══════════════════════════════════════════
    // ❌ 실패 시리즈
    // ══════════════════════════════════════════

    public static Result NotFound(string msg = "요청하신 데이터를 찾을 수 없습니다.")
        => new() { IsSuccess = false, Message = msg, Code = RsCode.NotFound };

    public static Result Forbidden(string msg = "해당 작업에 대한 권한이 없습니다.")
        => new() { IsSuccess = false, Message = msg, Code = RsCode.Forbidden };

    public static Result Conflict(string msg = "이미 존재하는 데이터와 충돌이 발생했습니다.")
        => new() { IsSuccess = false, Message = msg, Code = RsCode.Conflict };

    public static Result Failure(RsCode code, string msg)
        => new() { IsSuccess = false, Message = msg, Code = code };

    public static Result<FileData> Error(string msg = "서버 내부 오류가 발생했습니다.")
        => new() { IsSuccess = false, Message = msg, Code = RsCode.Error };

    public static Result ValidationFailed(
        IDictionary<string, string[]> errors,
        string msg = "입력값이 유효하지 않습니다.")
        => new()
        {
            IsSuccess = false,
            Message = msg,
            Code = RsCode.BadRequest,
            ValidationErrors = errors
        };

    // ══════════════════════════════════════════
    // 🔧 유틸
    // ══════════════════════════════════════════

    public Result WithTraceId(string traceId) => this with { TraceId = traceId };
}

// ====================================================
// Result<T> - 데이터 있는 응답 전용
// ====================================================
public record Result<T> : Result
{
    public T? Content { get; init; }

    // 성공 시 T 데이터를 담는 정적 메서드 (이건 반드시 필요!)
    public static Result<T> Success(T content, object? meta = null, string msg = "Ok")
    {
        return new Result<T>
        {
            IsSuccess = true,
            Content = content,
            Meta = meta,
            Message = msg,
            Code = RsCode.Ok
        };
    }

    // (사용 예시) return fileData;
    public static implicit operator Result<T>(T value)
    {
        return Success(value);
    }


    // ── 성공 시리즈 ──────────────────────────────────

    public static Result<T> Created(T? content, string msg = "새로운 리소스가 생성되었습니다.")
    {
        return new Result<T> { IsSuccess = true, Content = content, Message = msg, Code = RsCode.Created };
    }

    public new static Result<T> NoContent(string msg = "요청이 성공적으로 처리되었으며 반환할 데이터가 없습니다.")
    {
        return new Result<T> { IsSuccess = true, Content = default, Message = msg, Code = RsCode.NoContent };
    }

    public static Result<T> Updated(T? content, string msg = "정보가 성공적으로 수정되었습니다.")
    {
        return new Result<T> { IsSuccess = true, Content = content, Message = msg, Code = RsCode.Ok };
    }

    public new static Result<T> Accepted(string msg = "요청이 접수되어 처리 중입니다.")
    {
        return new Result<T> { IsSuccess = true, Code = RsCode.Accepted, Message = msg };
    }

    // ── 실패 시리즈 ──────────────────────────────────

    public new static Result<T> NotFound(string msg = "요청하신 데이터를 찾을 수 없습니다.")
    {
        return ToGeneric(Result.NotFound(msg));
    }

    public new static Result<T> Forbidden(string msg = "해당 작업에 대한 권한이 없습니다.")
    {
        return ToGeneric(Result.Forbidden(msg));
    }

    public new static Result<T> Conflict(string msg = "이미 존재하는 데이터와 충돌이 발생했습니다.")
    {
        return ToGeneric(Result.Conflict(msg));
    }

    public new static Result<T> Failure(RsCode code, string msg)
    {
        return ToGeneric(Result.Failure(code, msg));
    }

    public new static Result<T> Error(string msg = "서버 내부 오류가 발생했습니다.")
    {
        return ToGeneric(Result.Error(msg));
    }

    // 🆕 유효성 검사 실패 전용
    public new static Result<T> ValidationFailed(
        IDictionary<string, string[]> errors,
        string msg = "입력값이 유효하지 않습니다.")
    {
        return new Result<T>
        {
            IsSuccess = false,
            Message = msg,
            Code = RsCode.BadRequest,
            ValidationErrors = errors
        };
    }

    // 🆕 에러 추적 ID 첨부 (미들웨어 연동용)
    public new Result<T> WithTraceId(string traceId)
    {
        return this with { TraceId = traceId };
    }

    // 💡 헬퍼 메서드: 부가 정보를 그대로 복사하면서 타입만 변환
    private static Result<T> ToGeneric(Result result)
    {
        return new Result<T>
        {
            IsSuccess = result.IsSuccess,
            Message = result.Message,
            Code = result.Code,
            ProcessedAt = result.ProcessedAt,
            ValidationErrors = result.ValidationErrors,
            TraceId = result.TraceId
        };
    }
}
