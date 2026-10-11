using AgentMemory.Abstractions.Domain;
using AgentMemory.AgentFramework.Mapping;
using AgentMemory.AgentFramework.Security;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace AgentMemory.AgentFramework;

/// <summary>Projects outstanding user turns through the same admission rules as stored history.</summary>
internal static class PendingTurnContext
{
    internal static IReadOnlyList<Message> Snapshot(IBackgroundExtraction? queue, AgentFrameworkOptions options,
        string? applicationId, string? userId, string sessionId) =>
        options.IncludePendingTurns && options.ExtractInBackground && queue is BackgroundExtractionQueue builtIn
            ? builtIn.GetPendingTurns(applicationId, userId, sessionId)
            : [];

    internal static IReadOnlyList<ChatMessage> Append(IReadOnlyList<Message> pending,
        IReadOnlyList<ChatMessage> existing, IEnumerable<ChatMessage> liveThread, ContextFormatOptions format,
        IMemoryContextAdmissionPolicy admission, ILogger logger, bool mark)
    {
        if (pending.Count == 0 || format.MaxChatHistoryMessages <= 0) return existing;
        try
        {
            var seen = existing.Concat(liveThread).Select(m => MafTypeMapper.NormalizeForDedup(m.Text))
                .ToHashSet(StringComparer.Ordinal);
            var candidates = pending.Where(m => seen.Add(MafTypeMapper.NormalizeForDedup(m.Content)));
            var admitted = MafTypeMapper.ToGatedChatMessages(candidates, format, admission, logger)
                .Take(format.MaxChatHistoryMessages).Reverse().ToList();
            if (admitted.Count == 0) return existing;
            // Ordinary user turns, framed as reference data; never promote them to verified facts or instructions.
            var framed = RecalledTurns.Frame(admitted, ChatRole.User);
            return [.. existing, .. mark ? framed : RecalledTurns.Unmark(framed)];
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Match ordinary recall's failure boundary without bypassing a failing admission policy.
            logger.LogWarning(ex, "Pending-turn context could not be rendered; retaining the assembled context.");
            return existing;
        }
    }
}
