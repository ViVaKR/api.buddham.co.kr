namespace Buddham.Core.Schemas;


public static class SutraCopyingSchema
{
    public record Response
    {
        public long Id { get; set; }
        public required string Title { get; set; }
        public string? Section { get; set; }
        public required string Sutra { get; set; }
        public required Guid UserId { get; set; }
        public string? Pseudonym { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDelete { get; set; }
    }

    public record Request
    {
        public required string Title { get; set; }
        public string? Section { get; set; }
        public required string Sutra { get; set; }
    }
}
