using Buddham.Core.Common.Primitives;

namespace Buddham.Core.Entities.Ai;

public class ChatSession : EntityBase
{
    public Guid UserId { get; set; }
    public required string? Title { get; set; }
    public bool IsPublic { get; set; }

    // 🔍 풀텍스트 검색 (대화 내용 검색)
    public string? SearchVector { get; set; }

    // 🤖 의미론적 검색 (assistant 메시지만 임베딩)
    public float[]? Embedding { get; set; }
    public virtual ICollection<ChatMessage> Messages { get; set; } = [];
}