namespace Buddham.Core.Interfaces;

public interface ISoftDeletable
{
    DateTime? DeletedAt { get; set; }
    bool IsDeleted => DeletedAt.HasValue;
}
