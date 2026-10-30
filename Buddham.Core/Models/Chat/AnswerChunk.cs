namespace Buddham.Core.Models.Chat;

public readonly record struct AnswerChunk(string Text, bool IsThinking = false);
