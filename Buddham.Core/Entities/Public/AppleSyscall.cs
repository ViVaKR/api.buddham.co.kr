using Buddham.Core.Common.Primitives;

namespace Buddham.Core.Entities.Public;

public class AppleSyscall : EntityBase
{
    public int SyscallNumber { get; set; }
    public required string ReturnType { get; set; }
    public required string FuncName { get; set; }
    public required string Arguments { get; set; }

    /// <summary>
    /// ARM64 어셈블리 호출 예시
    /// </summary>
    public string? ExampleAsm { get; set; }

    /// <summary>
    /// C 레벨 함수 호출 예시
    /// </summary>
    public string? ExampleC { get; set; }

    public string? ExampleCSharp { get; set; }

    /// <summary>
    /// 이 syscall 이 멀 하는지 한 줄 설명
    /// </summary>
    public string? Description { get; set; }
}
