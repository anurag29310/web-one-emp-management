using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Persistence.Repositories
{
    public class MessagingRepository : IMessagingRepository
    {
        private readonly ApplicationDbContext _db;

        public MessagingRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        // ─── Conversations ─────────────────────────────────────────────────────────

        public async Task<Conversation?> GetConversationByIdAsync(Guid id, CancellationToken ct = default) =>
            await _db.Conversations.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, ct);

        public async Task<Conversation?> GetConversationByIdIncludingDeletedAsync(Guid id, CancellationToken ct = default) =>
            await _db.Conversations.FirstOrDefaultAsync(c => c.Id == id, ct);

        // Tracked (not AsNoTracking): the caller updates and saves this same instance when reusing
        // an existing direct conversation, so it must come back as a tracked entity.
        public async Task<Conversation?> FindDirectConversationAsync(Guid userAId, Guid userBId, CancellationToken ct = default) =>
            await _db.Conversations
                .Where(c => !c.IsDeleted && !c.IsGroup)
                .Where(c => _db.MessageParticipants.Count(p => p.ConversationId == c.Id && p.LeftAtUtc == null) == 2
                    && _db.MessageParticipants.Any(p => p.ConversationId == c.Id && p.UserId == userAId && p.LeftAtUtc == null)
                    && _db.MessageParticipants.Any(p => p.ConversationId == c.Id && p.UserId == userBId && p.LeftAtUtc == null))
                .FirstOrDefaultAsync(ct);

        private IQueryable<Conversation> BuildUserConversationsQuery(Guid userId, string? search)
        {
            var q = _db.Conversations.AsNoTracking()
                .Where(c => !c.IsDeleted)
                .Where(c => _db.MessageParticipants.Any(p => p.ConversationId == c.Id && p.UserId == userId && p.LeftAtUtc == null));

            if (!string.IsNullOrWhiteSpace(search))
            {
                q = q.Where(c =>
                    (c.Title != null && c.Title.Contains(search)) ||
                    _db.MessageParticipants.Any(p => p.ConversationId == c.Id && p.UserId != userId && p.LeftAtUtc == null &&
                        _db.Users.Any(u => u.Id == p.UserId &&
                            _db.Employees.Any(e => e.Id == u.EmployeeId && (e.FirstName.Contains(search) || e.LastName.Contains(search))))));
            }

            return q;
        }

        public async Task<IEnumerable<Conversation>> GetConversationsForUserAsync(Guid userId, int page, int pageSize, string? search, CancellationToken ct = default) =>
            await BuildUserConversationsQuery(userId, search)
                .OrderByDescending(c => c.LastMessageAtUtc ?? c.CreatedAtUtc)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync(ct);

        public async Task<int> CountConversationsForUserAsync(Guid userId, string? search, CancellationToken ct = default) =>
            await BuildUserConversationsQuery(userId, search).CountAsync(ct);

        public async Task<int> CountUnreadConversationsForUserAsync(Guid userId, CancellationToken ct = default) =>
            await _db.MessageParticipants.AsNoTracking()
                .Where(p => p.UserId == userId && p.LeftAtUtc == null)
                .Where(p => _db.Messages.Any(m => m.ConversationId == p.ConversationId && (p.LastReadAtUtc == null || m.SentAtUtc > p.LastReadAtUtc))
                    && _db.Conversations.Any(c => c.Id == p.ConversationId && !c.IsDeleted))
                .CountAsync(ct);

        public async Task AddConversationAsync(Conversation conversation, CancellationToken ct = default) =>
            await _db.Conversations.AddAsync(conversation, ct);

        public Task UpdateConversationAsync(Conversation conversation, CancellationToken ct = default)
        {
            _db.Conversations.Update(conversation);
            return Task.CompletedTask;
        }

        public Task DeleteConversationAsync(Conversation conversation, CancellationToken ct = default)
        {
            conversation.IsDeleted = true;
            _db.Conversations.Update(conversation);
            return Task.CompletedTask;
        }

        public Task RestoreConversationAsync(Conversation conversation, CancellationToken ct = default)
        {
            conversation.IsDeleted = false;
            _db.Conversations.Update(conversation);
            return Task.CompletedTask;
        }

        // ─── Participants ──────────────────────────────────────────────────────────

        public async Task<MessageParticipant?> GetParticipantAsync(Guid conversationId, Guid userId, CancellationToken ct = default) =>
            await _db.MessageParticipants.FirstOrDefaultAsync(p => p.ConversationId == conversationId && p.UserId == userId, ct);

        public async Task<IReadOnlyList<MessageParticipant>> GetActiveParticipantsAsync(Guid conversationId, CancellationToken ct = default) =>
            await _db.MessageParticipants.AsNoTracking()
                .Where(p => p.ConversationId == conversationId && p.LeftAtUtc == null)
                .ToListAsync(ct);

        public async Task<int> CountActiveParticipantsAsync(Guid conversationId, CancellationToken ct = default) =>
            await _db.MessageParticipants.CountAsync(p => p.ConversationId == conversationId && p.LeftAtUtc == null, ct);

        public async Task<bool> IsActiveParticipantAsync(Guid conversationId, Guid userId, CancellationToken ct = default) =>
            await _db.MessageParticipants.AnyAsync(p => p.ConversationId == conversationId && p.UserId == userId && p.LeftAtUtc == null, ct);

        public async Task AddParticipantAsync(MessageParticipant participant, CancellationToken ct = default) =>
            await _db.MessageParticipants.AddAsync(participant, ct);

        public Task UpdateParticipantAsync(MessageParticipant participant, CancellationToken ct = default)
        {
            _db.MessageParticipants.Update(participant);
            return Task.CompletedTask;
        }

        public async Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(IEnumerable<Guid> userIds, CancellationToken ct = default)
        {
            var ids = userIds.Distinct().ToList();
            if (ids.Count == 0) return new Dictionary<Guid, string>();

            return await _db.Users.AsNoTracking()
                .Where(u => ids.Contains(u.Id))
                .Select(u => new
                {
                    u.Id,
                    Name = _db.Employees.Where(e => e.Id == u.EmployeeId).Select(e => e.FirstName + " " + e.LastName).FirstOrDefault() ?? u.UserName
                })
                .ToDictionaryAsync(x => x.Id, x => x.Name, ct);
        }

        // ─── Messages ──────────────────────────────────────────────────────────────

        public async Task<IEnumerable<Message>> GetMessagesAsync(Guid conversationId, int page, int pageSize, CancellationToken ct = default) =>
            await _db.Messages.AsNoTracking()
                .Where(m => m.ConversationId == conversationId)
                .OrderByDescending(m => m.SentAtUtc)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync(ct);

        public async Task<int> CountMessagesAsync(Guid conversationId, CancellationToken ct = default) =>
            await _db.Messages.CountAsync(m => m.ConversationId == conversationId, ct);

        public async Task<Message?> GetLastMessageAsync(Guid conversationId, CancellationToken ct = default) =>
            await _db.Messages.AsNoTracking()
                .Where(m => m.ConversationId == conversationId)
                .OrderByDescending(m => m.SentAtUtc)
                .FirstOrDefaultAsync(ct);

        public async Task<int> CountUnreadAsync(Guid conversationId, DateTime? afterExclusive, CancellationToken ct = default) =>
            await _db.Messages.CountAsync(m => m.ConversationId == conversationId && (afterExclusive == null || m.SentAtUtc > afterExclusive), ct);

        public async Task AddMessageAsync(Message message, CancellationToken ct = default) =>
            await _db.Messages.AddAsync(message, ct);

        public async Task SaveChangesAsync(CancellationToken ct = default) =>
            await _db.SaveChangesAsync(ct);
    }
}
