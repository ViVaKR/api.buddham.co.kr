namespace Buddham.Core.Interfaces.Common;

public interface ICursorKeyed
{
    long Id { get; }
    DateTime CreatedAt { get; }
}
