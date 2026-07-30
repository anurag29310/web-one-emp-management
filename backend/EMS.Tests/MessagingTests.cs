using EMS.Application.Features.Messaging.Commands;
using EMS.Application.Features.Messaging.Handlers;
using EMS.Application.Features.Messaging.Queries;
using EMS.Application.Features.Messaging.Validators;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Persistence.Context;
using EMS.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EMS.Tests
{
    public class MessagingTests
    {
        private class RecordingAuditLogger : IAuditLogger
        {
            public Task LogAsync(string entityName, Guid? entityId, string action, object? oldValues = null, object? newValues = null, CancellationToken ct = default)
                => Task.CompletedTask;
        }

        private static ApplicationDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("ems_messaging_test_" + Guid.NewGuid())
                .Options;
            return new ApplicationDbContext(options);
        }

        private static async Task<User> SeedUserAsync(ApplicationDbContext db)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = "user_" + Guid.NewGuid().ToString("N")[..8],
                Email = Guid.NewGuid() + "@test.local",
                PasswordHash = "hash",
                IsActive = true
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            return user;
        }

        private static (MessagingRepository repo, IUserRepository userRepo) CreateRepos(ApplicationDbContext db) =>
            (new MessagingRepository(db), new UserRepository(db));

        // ─── CreateConversation ────────────────────────────────────────────────────

        [Fact]
        public async Task CreateConversation_WithOneOtherParticipant_CreatesDirectConversationWithInitialMessage()
        {
            using var db = CreateDb();
            var (repo, _) = CreateRepos(db);
            var alice = await SeedUserAsync(db);
            var bob = await SeedUserAsync(db);

            var handler = new CreateConversationCommandHandler(repo, new RecordingAuditLogger(), NullLogger<CreateConversationCommandHandler>.Instance);

            var conversationId = await handler.Handle(new CreateConversationCommand
            {
                RequestingUserId = alice.Id,
                ParticipantUserIds = new() { bob.Id },
                InitialMessageBody = "Hey Bob!"
            }, CancellationToken.None);

            var conversation = await repo.GetConversationByIdAsync(conversationId, CancellationToken.None);
            Assert.NotNull(conversation);
            Assert.False(conversation!.IsGroup);

            var participants = await repo.GetActiveParticipantsAsync(conversationId, CancellationToken.None);
            Assert.Equal(2, participants.Count);

            var message = await repo.GetLastMessageAsync(conversationId, CancellationToken.None);
            Assert.Equal("Hey Bob!", message!.Body);
        }

        [Fact]
        public async Task CreateConversation_CalledTwiceForSamePair_ReusesExistingConversation()
        {
            using var db = CreateDb();
            var (repo, _) = CreateRepos(db);
            var alice = await SeedUserAsync(db);
            var bob = await SeedUserAsync(db);
            var handler = new CreateConversationCommandHandler(repo, new RecordingAuditLogger(), NullLogger<CreateConversationCommandHandler>.Instance);

            var firstId = await handler.Handle(new CreateConversationCommand
            {
                RequestingUserId = alice.Id,
                ParticipantUserIds = new() { bob.Id },
                InitialMessageBody = "First message"
            }, CancellationToken.None);

            var secondId = await handler.Handle(new CreateConversationCommand
            {
                RequestingUserId = alice.Id,
                ParticipantUserIds = new() { bob.Id },
                InitialMessageBody = "Second message"
            }, CancellationToken.None);

            Assert.Equal(firstId, secondId);
            var total = await repo.CountMessagesAsync(firstId, CancellationToken.None);
            Assert.Equal(2, total);
        }

        [Fact]
        public async Task CreateConversation_WithTwoOtherParticipants_CreatesGroupConversation()
        {
            using var db = CreateDb();
            var (repo, _) = CreateRepos(db);
            var alice = await SeedUserAsync(db);
            var bob = await SeedUserAsync(db);
            var carol = await SeedUserAsync(db);
            var handler = new CreateConversationCommandHandler(repo, new RecordingAuditLogger(), NullLogger<CreateConversationCommandHandler>.Instance);

            var conversationId = await handler.Handle(new CreateConversationCommand
            {
                RequestingUserId = alice.Id,
                ParticipantUserIds = new() { bob.Id, carol.Id },
                InitialMessageBody = "Team chat"
            }, CancellationToken.None);

            var conversation = await repo.GetConversationByIdAsync(conversationId, CancellationToken.None);
            Assert.True(conversation!.IsGroup);
            var participants = await repo.GetActiveParticipantsAsync(conversationId, CancellationToken.None);
            Assert.Equal(3, participants.Count);
        }

        // ─── SendMessage ───────────────────────────────────────────────────────────

        [Fact]
        public async Task SendMessage_ByActiveParticipant_Succeeds()
        {
            using var db = CreateDb();
            var (repo, _) = CreateRepos(db);
            var alice = await SeedUserAsync(db);
            var bob = await SeedUserAsync(db);
            var createHandler = new CreateConversationCommandHandler(repo, new RecordingAuditLogger(), NullLogger<CreateConversationCommandHandler>.Instance);
            var conversationId = await createHandler.Handle(new CreateConversationCommand
            {
                RequestingUserId = alice.Id,
                ParticipantUserIds = new() { bob.Id },
                InitialMessageBody = "Hi"
            }, CancellationToken.None);

            var sendHandler = new SendMessageCommandHandler(repo, new RecordingAuditLogger(), NullLogger<SendMessageCommandHandler>.Instance);
            var messageId = await sendHandler.Handle(new SendMessageCommand
            {
                ConversationId = conversationId,
                RequestingUserId = bob.Id,
                Body = "Hi back!"
            }, CancellationToken.None);

            Assert.NotEqual(Guid.Empty, messageId);
            Assert.Equal(2, await repo.CountMessagesAsync(conversationId, CancellationToken.None));
        }

        [Fact]
        public async Task SendMessage_ByNonParticipant_ThrowsUnauthorized()
        {
            using var db = CreateDb();
            var (repo, _) = CreateRepos(db);
            var alice = await SeedUserAsync(db);
            var bob = await SeedUserAsync(db);
            var outsider = await SeedUserAsync(db);
            var createHandler = new CreateConversationCommandHandler(repo, new RecordingAuditLogger(), NullLogger<CreateConversationCommandHandler>.Instance);
            var conversationId = await createHandler.Handle(new CreateConversationCommand
            {
                RequestingUserId = alice.Id,
                ParticipantUserIds = new() { bob.Id },
                InitialMessageBody = "Hi"
            }, CancellationToken.None);

            var sendHandler = new SendMessageCommandHandler(repo, new RecordingAuditLogger(), NullLogger<SendMessageCommandHandler>.Instance);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sendHandler.Handle(new SendMessageCommand
            {
                ConversationId = conversationId,
                RequestingUserId = outsider.Id,
                Body = "I shouldn't be able to send this"
            }, CancellationToken.None));
        }

        [Fact]
        public async Task SendMessage_ToNonexistentConversation_ThrowsInvalidOperation()
        {
            using var db = CreateDb();
            var (repo, _) = CreateRepos(db);
            var alice = await SeedUserAsync(db);
            var sendHandler = new SendMessageCommandHandler(repo, new RecordingAuditLogger(), NullLogger<SendMessageCommandHandler>.Instance);

            await Assert.ThrowsAsync<InvalidOperationException>(() => sendHandler.Handle(new SendMessageCommand
            {
                ConversationId = Guid.NewGuid(),
                RequestingUserId = alice.Id,
                Body = "Hello?"
            }, CancellationToken.None));
        }

        // ─── Unread tracking ───────────────────────────────────────────────────────

        [Fact]
        public async Task GetConversations_RecipientSeesUnreadMessage_SenderDoesNot()
        {
            using var db = CreateDb();
            var (repo, _) = CreateRepos(db);
            var alice = await SeedUserAsync(db);
            var bob = await SeedUserAsync(db);
            var createHandler = new CreateConversationCommandHandler(repo, new RecordingAuditLogger(), NullLogger<CreateConversationCommandHandler>.Instance);
            await createHandler.Handle(new CreateConversationCommand
            {
                RequestingUserId = alice.Id,
                ParticipantUserIds = new() { bob.Id },
                InitialMessageBody = "Unread for Bob"
            }, CancellationToken.None);

            var queryHandler = new GetConversationsQueryHandler(repo);

            var aliceView = await queryHandler.Handle(new GetConversationsQuery { RequestingUserId = alice.Id }, CancellationToken.None);
            Assert.Equal(0, aliceView.Data.Single().UnreadCount);

            var bobView = await queryHandler.Handle(new GetConversationsQuery { RequestingUserId = bob.Id }, CancellationToken.None);
            Assert.Equal(1, bobView.Data.Single().UnreadCount);
        }

        [Fact]
        public async Task MarkConversationRead_ResetsUnreadCountToZero()
        {
            using var db = CreateDb();
            var (repo, _) = CreateRepos(db);
            var alice = await SeedUserAsync(db);
            var bob = await SeedUserAsync(db);
            var createHandler = new CreateConversationCommandHandler(repo, new RecordingAuditLogger(), NullLogger<CreateConversationCommandHandler>.Instance);
            var conversationId = await createHandler.Handle(new CreateConversationCommand
            {
                RequestingUserId = alice.Id,
                ParticipantUserIds = new() { bob.Id },
                InitialMessageBody = "Please read me"
            }, CancellationToken.None);

            var markReadHandler = new MarkConversationReadCommandHandler(repo);
            await markReadHandler.Handle(new MarkConversationReadCommand { ConversationId = conversationId, RequestingUserId = bob.Id }, CancellationToken.None);

            var queryHandler = new GetConversationsQueryHandler(repo);
            var bobView = await queryHandler.Handle(new GetConversationsQuery { RequestingUserId = bob.Id }, CancellationToken.None);
            Assert.Equal(0, bobView.Data.Single().UnreadCount);
        }

        [Fact]
        public async Task GetUnreadConversationCount_CountsOnlyConversationsWithUnreadMessages()
        {
            using var db = CreateDb();
            var (repo, _) = CreateRepos(db);
            var alice = await SeedUserAsync(db);
            var bob = await SeedUserAsync(db);
            var carol = await SeedUserAsync(db);
            var createHandler = new CreateConversationCommandHandler(repo, new RecordingAuditLogger(), NullLogger<CreateConversationCommandHandler>.Instance);

            var readConversationId = await createHandler.Handle(new CreateConversationCommand { RequestingUserId = alice.Id, ParticipantUserIds = new() { bob.Id }, InitialMessageBody = "msg1" }, CancellationToken.None);
            await createHandler.Handle(new CreateConversationCommand { RequestingUserId = carol.Id, ParticipantUserIds = new() { bob.Id }, InitialMessageBody = "msg2" }, CancellationToken.None);

            var markReadHandler = new MarkConversationReadCommandHandler(repo);
            await markReadHandler.Handle(new MarkConversationReadCommand { ConversationId = readConversationId, RequestingUserId = bob.Id }, CancellationToken.None);

            var countHandler = new GetUnreadConversationCountQueryHandler(repo);
            var count = await countHandler.Handle(new GetUnreadConversationCountQuery { RequestingUserId = bob.Id }, CancellationToken.None);

            Assert.Equal(1, count);
        }

        // ─── AddParticipants / Leave ───────────────────────────────────────────────

        [Fact]
        public async Task AddParticipants_ToDirectConversation_PromotesToGroupAndAllowsNewMember()
        {
            using var db = CreateDb();
            var (repo, _) = CreateRepos(db);
            var alice = await SeedUserAsync(db);
            var bob = await SeedUserAsync(db);
            var carol = await SeedUserAsync(db);
            var createHandler = new CreateConversationCommandHandler(repo, new RecordingAuditLogger(), NullLogger<CreateConversationCommandHandler>.Instance);
            var conversationId = await createHandler.Handle(new CreateConversationCommand
            {
                RequestingUserId = alice.Id,
                ParticipantUserIds = new() { bob.Id },
                InitialMessageBody = "Just us two"
            }, CancellationToken.None);

            var addHandler = new AddParticipantsCommandHandler(repo, new RecordingAuditLogger(), NullLogger<AddParticipantsCommandHandler>.Instance);
            await addHandler.Handle(new AddParticipantsCommand
            {
                ConversationId = conversationId,
                RequestingUserId = alice.Id,
                UserIds = new() { carol.Id }
            }, CancellationToken.None);

            var conversation = await repo.GetConversationByIdAsync(conversationId, CancellationToken.None);
            Assert.True(conversation!.IsGroup);

            var sendHandler = new SendMessageCommandHandler(repo, new RecordingAuditLogger(), NullLogger<SendMessageCommandHandler>.Instance);
            var messageId = await sendHandler.Handle(new SendMessageCommand { ConversationId = conversationId, RequestingUserId = carol.Id, Body = "Hi, I'm new here" }, CancellationToken.None);
            Assert.NotEqual(Guid.Empty, messageId);
        }

        [Fact]
        public async Task LeaveConversation_OnDirectConversation_ThrowsInvalidOperation()
        {
            using var db = CreateDb();
            var (repo, _) = CreateRepos(db);
            var alice = await SeedUserAsync(db);
            var bob = await SeedUserAsync(db);
            var createHandler = new CreateConversationCommandHandler(repo, new RecordingAuditLogger(), NullLogger<CreateConversationCommandHandler>.Instance);
            var conversationId = await createHandler.Handle(new CreateConversationCommand
            {
                RequestingUserId = alice.Id,
                ParticipantUserIds = new() { bob.Id },
                InitialMessageBody = "Hi"
            }, CancellationToken.None);

            var leaveHandler = new LeaveConversationCommandHandler(repo, new RecordingAuditLogger(), NullLogger<LeaveConversationCommandHandler>.Instance);

            await Assert.ThrowsAsync<InvalidOperationException>(() => leaveHandler.Handle(new LeaveConversationCommand
            {
                ConversationId = conversationId,
                RequestingUserId = alice.Id
            }, CancellationToken.None));
        }

        [Fact]
        public async Task LeaveConversation_OnGroupConversation_PreventsFurtherMessaging()
        {
            using var db = CreateDb();
            var (repo, _) = CreateRepos(db);
            var alice = await SeedUserAsync(db);
            var bob = await SeedUserAsync(db);
            var carol = await SeedUserAsync(db);
            var createHandler = new CreateConversationCommandHandler(repo, new RecordingAuditLogger(), NullLogger<CreateConversationCommandHandler>.Instance);
            var conversationId = await createHandler.Handle(new CreateConversationCommand
            {
                RequestingUserId = alice.Id,
                ParticipantUserIds = new() { bob.Id, carol.Id },
                InitialMessageBody = "Group chat"
            }, CancellationToken.None);

            var leaveHandler = new LeaveConversationCommandHandler(repo, new RecordingAuditLogger(), NullLogger<LeaveConversationCommandHandler>.Instance);
            await leaveHandler.Handle(new LeaveConversationCommand { ConversationId = conversationId, RequestingUserId = carol.Id }, CancellationToken.None);

            var sendHandler = new SendMessageCommandHandler(repo, new RecordingAuditLogger(), NullLogger<SendMessageCommandHandler>.Instance);
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sendHandler.Handle(new SendMessageCommand
            {
                ConversationId = conversationId,
                RequestingUserId = carol.Id,
                Body = "I already left"
            }, CancellationToken.None));
        }

        // ─── Delete / Restore ──────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteConversation_ThenRestore_RoundTrips()
        {
            using var db = CreateDb();
            var (repo, _) = CreateRepos(db);
            var alice = await SeedUserAsync(db);
            var bob = await SeedUserAsync(db);
            var createHandler = new CreateConversationCommandHandler(repo, new RecordingAuditLogger(), NullLogger<CreateConversationCommandHandler>.Instance);
            var conversationId = await createHandler.Handle(new CreateConversationCommand
            {
                RequestingUserId = alice.Id,
                ParticipantUserIds = new() { bob.Id },
                InitialMessageBody = "Hi"
            }, CancellationToken.None);

            var deleteHandler = new DeleteConversationCommandHandler(repo, new RecordingAuditLogger(), NullLogger<DeleteConversationCommandHandler>.Instance);
            await deleteHandler.Handle(new DeleteConversationCommand { Id = conversationId, RequestingUserId = alice.Id }, CancellationToken.None);

            Assert.Null(await repo.GetConversationByIdAsync(conversationId, CancellationToken.None));

            var restoreHandler = new RestoreConversationCommandHandler(repo, new RecordingAuditLogger(), NullLogger<RestoreConversationCommandHandler>.Instance);
            await restoreHandler.Handle(new RestoreConversationCommand { Id = conversationId, RequestingUserId = alice.Id }, CancellationToken.None);

            Assert.NotNull(await repo.GetConversationByIdAsync(conversationId, CancellationToken.None));
        }

        [Fact]
        public async Task RestoreConversation_WhenNotDeleted_ThrowsInvalidOperation()
        {
            using var db = CreateDb();
            var (repo, _) = CreateRepos(db);
            var alice = await SeedUserAsync(db);
            var bob = await SeedUserAsync(db);
            var createHandler = new CreateConversationCommandHandler(repo, new RecordingAuditLogger(), NullLogger<CreateConversationCommandHandler>.Instance);
            var conversationId = await createHandler.Handle(new CreateConversationCommand
            {
                RequestingUserId = alice.Id,
                ParticipantUserIds = new() { bob.Id },
                InitialMessageBody = "Hi"
            }, CancellationToken.None);

            var restoreHandler = new RestoreConversationCommandHandler(repo, new RecordingAuditLogger(), NullLogger<RestoreConversationCommandHandler>.Instance);

            await Assert.ThrowsAsync<InvalidOperationException>(() => restoreHandler.Handle(new RestoreConversationCommand
            {
                Id = conversationId,
                RequestingUserId = alice.Id
            }, CancellationToken.None));
        }

        // ─── Validators ────────────────────────────────────────────────────────────

        [Fact]
        public async Task CreateConversationCommandValidator_WithNoParticipants_Fails()
        {
            using var db = CreateDb();
            var (_, userRepo) = CreateRepos(db);
            var validator = new CreateConversationCommandValidator(userRepo);

            var result = await validator.ValidateAsync(new CreateConversationCommand
            {
                RequestingUserId = Guid.NewGuid(),
                ParticipantUserIds = new(),
                InitialMessageBody = "Hi"
            });

            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task CreateConversationCommandValidator_WithNonexistentParticipant_Fails()
        {
            using var db = CreateDb();
            var (_, userRepo) = CreateRepos(db);
            var validator = new CreateConversationCommandValidator(userRepo);

            var result = await validator.ValidateAsync(new CreateConversationCommand
            {
                RequestingUserId = Guid.NewGuid(),
                ParticipantUserIds = new() { Guid.NewGuid() },
                InitialMessageBody = "Hi"
            });

            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task CreateConversationCommandValidator_WithValidExistingParticipant_Passes()
        {
            using var db = CreateDb();
            var (_, userRepo) = CreateRepos(db);
            var bob = await SeedUserAsync(db);
            var validator = new CreateConversationCommandValidator(userRepo);

            var result = await validator.ValidateAsync(new CreateConversationCommand
            {
                RequestingUserId = Guid.NewGuid(),
                ParticipantUserIds = new() { bob.Id },
                InitialMessageBody = "Hi"
            });

            Assert.True(result.IsValid);
        }
    }
}
