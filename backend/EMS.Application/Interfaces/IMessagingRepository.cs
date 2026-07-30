using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Interfaces
{
    public interface IMessagingRepository
    {
        // ─── Conversations ─────────────────────────────────────────────────────────
        Task<Conversation?> GetConversationByIdAsync(Guid id, CancellationToken ct = default);
        Task<Conversation?> GetConversationByIdIncludingDeletedAsync(Guid id, CancellationToken ct = default);

        /// <summary>Finds an existing, active (not left by either side) 1:1 conversation between exactly these two users, if one exists — used to avoid spawning duplicate DM threads.</summary>
        Task<Conversation?> FindDirectConversationAsync(Guid userAId, Guid userBId, CancellationToken ct = default);

        Task<IEnumerable<Conversation>> GetConversationsForUserAsync(Guid userId, int page, int pageSize, string? search, CancellationToken ct = default);
        Task<int> CountConversationsForUserAsync(Guid userId, string? search, CancellationToken ct = default);
        Task<int> CountUnreadConversationsForUserAsync(Guid userId, CancellationToken ct = default);

        Task AddConversationAsync(Conversation conversation, CancellationToken ct = default);
        Task UpdateConversationAsync(Conversation conversation, CancellationToken ct = default);
        Task DeleteConversationAsync(Conversation conversation, CancellationToken ct = default);
        Task RestoreConversationAsync(Conversation conversation, CancellationToken ct = default);

        // ─── Participants ──────────────────────────────────────────────────────────
        Task<MessageParticipant?> GetParticipantAsync(Guid conversationId, Guid userId, CancellationToken ct = default);
        Task<IReadOnlyList<MessageParticipant>> GetActiveParticipantsAsync(Guid conversationId, CancellationToken ct = default);
        Task<int> CountActiveParticipantsAsync(Guid conversationId, CancellationToken ct = default);
        Task<bool> IsActiveParticipantAsync(Guid conversationId, Guid userId, CancellationToken ct = default);
        Task AddParticipantAsync(MessageParticipant participant, CancellationToken ct = default);
        Task UpdateParticipantAsync(MessageParticipant participant, CancellationToken ct = default);

        /// <summary>Batch-resolves display names ("First Last") for the given user ids via their linked Employee record.</summary>
        Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(IEnumerable<Guid> userIds, CancellationToken ct = default);

        // ─── Messages ──────────────────────────────────────────────────────────────
        Task<IEnumerable<Message>> GetMessagesAsync(Guid conversationId, int page, int pageSize, CancellationToken ct = default);
        Task<int> CountMessagesAsync(Guid conversationId, CancellationToken ct = default);
        Task<Message?> GetLastMessageAsync(Guid conversationId, CancellationToken ct = default);
        Task<int> CountUnreadAsync(Guid conversationId, DateTime? afterExclusive, CancellationToken ct = default);
        Task AddMessageAsync(Message message, CancellationToken ct = default);

        Task SaveChangesAsync(CancellationToken ct = default);
    }
}
