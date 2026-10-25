using Buddham.Core.EnumTypes;

namespace Buddham.Core.Schemas;

public class ReactionSchema
{
    public record Response
    {
        public ReactionType CurrentReaction { get; set; }
        public int HapjangCount { get; set; }
        public int LotusCount { get; set; }
        public int MeritCount { get; set; }
    }
}