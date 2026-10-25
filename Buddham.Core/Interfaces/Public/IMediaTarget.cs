namespace Buddham.Core.Interfaces.Public;

public interface IMediaTarget
{
    long TargetId { get; set; }
    static abstract string TargetColumnPropertyName { get; }
}