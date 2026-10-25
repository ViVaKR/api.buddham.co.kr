namespace Buddham.Core.Schemas;

public static class AppleSyscallSchema
{
    public record Response
    {
        public long Id { get; set; }
        public int SyscallNumber { get; set; }
        public required string ReturnType { get; set; }
        public required string FuncName { get; set; }
        public required string Arguments { get; set; }
        public string? ExampleAsm { get; set; }
        public string? ExampleC { get; set; }
        public string? Description { get; set; }
    }

    public record Request
    {
        public string? ExampleAsm { get; set; }
        public string? ExampleC { get; set; }
        public string? Description { get; set; }
    }
}
