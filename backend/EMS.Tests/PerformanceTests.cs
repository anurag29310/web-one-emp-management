using EMS.Application.Features.Performance.Commands;
using EMS.Application.Features.Performance.Handlers;
using EMS.Application.Features.Performance.Queries;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
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
    public class PerformanceTests
    {
        private class RecordingAuditLogger : IAuditLogger
        {
            public Task LogAsync(string entityName, Guid? entityId, string action, object? oldValues = null, object? newValues = null, CancellationToken ct = default)
                => Task.CompletedTask;
        }

        private static ApplicationDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("ems_performance_test_" + Guid.NewGuid())
                .Options;
            return new ApplicationDbContext(options);
        }

        private static async Task<Guid> SeedDesignationAsync(ApplicationDbContext db, string name = "Designation")
        {
            var id = Guid.NewGuid();
            db.Designations.Add(new Designation { Id = id, Name = name + "-" + id.ToString("N")[..8], Code = "DSG-" + id.ToString("N")[..8], CreatedAtUtc = DateTime.UtcNow });
            await db.SaveChangesAsync();
            return id;
        }

        private static async Task<Employee> SeedEmployeeAsync(ApplicationDbContext db, Guid? managerId = null, Guid? designationId = null)
        {
            var officeLocationId = Guid.NewGuid();
            db.OfficeLocations.Add(new OfficeLocation { Id = officeLocationId, Name = "Location-" + officeLocationId, Code = "LOC-" + officeLocationId.ToString("N")[..8], City = "City", Country = "Country", TimeZoneId = "UTC", CreatedAtUtc = DateTime.UtcNow });

            var employee = new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeCode = "EMP-" + Guid.NewGuid().ToString("N")[..8],
                FirstName = "Test",
                LastName = "Employee",
                JoinDate = DateTime.UtcNow.Date,
                IsActive = true,
                ManagerId = managerId,
                DesignationId = designationId ?? await SeedDesignationAsync(db),
                OfficeLocationId = officeLocationId
            };
            db.Employees.Add(employee);
            await db.SaveChangesAsync();
            return employee;
        }

        private static async Task<User> SeedUserAsync(ApplicationDbContext db, Guid? employeeId)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = "user_" + Guid.NewGuid().ToString("N")[..8],
                Email = Guid.NewGuid() + "@test.local",
                PasswordHash = "hash",
                EmployeeId = employeeId
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            return user;
        }

        // ─── Goals ─────────────────────────────────────────────────────────────────

        [Fact]
        public async Task CreateGoal_ByManagerForOwnDirectReport_Succeeds()
        {
            using var db = CreateDb();
            var manager = await SeedEmployeeAsync(db);
            var report = await SeedEmployeeAsync(db, manager.Id);
            var managerUser = await SeedUserAsync(db, manager.Id);

            var repo = new PerformanceRepository(db);
            var authRepo = new AuthRepository(db);
            var employeeRepo = new EmployeeRepository(db);
            var handler = new CreateGoalCommandHandler(repo, authRepo, employeeRepo, new RecordingAuditLogger(), NullLogger<CreateGoalCommandHandler>.Instance);

            var id = await handler.Handle(new CreateGoalCommand
            {
                EmployeeId = report.Id,
                Title = "Ship feature X",
                StartDate = new DateTime(2026, 1, 1),
                TargetDate = new DateTime(2026, 3, 31),
                RequestingUserId = managerUser.Id,
                IsPrivileged = false,
                IsManager = true
            }, CancellationToken.None);

            var goal = await repo.GetGoalByIdAsync(id, CancellationToken.None);
            Assert.NotNull(goal);
            Assert.StartsWith("GOL-", goal!.GoalNumber);
            Assert.Equal(GoalStatus.NotStarted, goal.Status);
        }

        [Fact]
        public async Task CreateGoal_ByManagerForNonReport_ThrowsUnauthorized()
        {
            using var db = CreateDb();
            var manager = await SeedEmployeeAsync(db);
            var unrelatedEmployee = await SeedEmployeeAsync(db);
            var managerUser = await SeedUserAsync(db, manager.Id);

            var repo = new PerformanceRepository(db);
            var authRepo = new AuthRepository(db);
            var employeeRepo = new EmployeeRepository(db);
            var handler = new CreateGoalCommandHandler(repo, authRepo, employeeRepo, new RecordingAuditLogger(), NullLogger<CreateGoalCommandHandler>.Instance);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(new CreateGoalCommand
            {
                EmployeeId = unrelatedEmployee.Id,
                Title = "Ship feature X",
                StartDate = new DateTime(2026, 1, 1),
                TargetDate = new DateTime(2026, 3, 31),
                RequestingUserId = managerUser.Id,
                IsPrivileged = false,
                IsManager = true
            }, CancellationToken.None));
        }

        [Fact]
        public async Task UpdateGoalProgress_ByOwningEmployee_Succeeds()
        {
            using var db = CreateDb();
            var employee = await SeedEmployeeAsync(db);
            var employeeUser = await SeedUserAsync(db, employee.Id);

            var repo = new PerformanceRepository(db);
            var authRepo = new AuthRepository(db);
            var employeeRepo = new EmployeeRepository(db);
            var createHandler = new CreateGoalCommandHandler(repo, authRepo, employeeRepo, new RecordingAuditLogger(), NullLogger<CreateGoalCommandHandler>.Instance);
            var progressHandler = new UpdateGoalProgressCommandHandler(repo, authRepo, employeeRepo, new RecordingAuditLogger(), NullLogger<UpdateGoalProgressCommandHandler>.Instance);

            var goalId = await createHandler.Handle(new CreateGoalCommand
            {
                EmployeeId = employee.Id,
                Title = "Improve test coverage",
                StartDate = new DateTime(2026, 1, 1),
                TargetDate = new DateTime(2026, 3, 31),
                RequestingUserId = employeeUser.Id,
                IsPrivileged = true // seed as privileged create; the point under test is the progress update below
            }, CancellationToken.None);

            await progressHandler.Handle(new UpdateGoalProgressCommand
            {
                Id = goalId,
                ProgressPercent = 40,
                RequestingUserId = employeeUser.Id,
                IsPrivileged = false,
                IsManager = false
            }, CancellationToken.None);

            var goal = await repo.GetGoalByIdAsync(goalId, CancellationToken.None);
            Assert.Equal(40, goal!.ProgressPercent);
        }

        [Fact]
        public async Task UpdateGoalProgress_ByUnrelatedEmployee_ThrowsUnauthorized()
        {
            using var db = CreateDb();
            var employee = await SeedEmployeeAsync(db);
            var otherEmployee = await SeedEmployeeAsync(db);
            var otherUser = await SeedUserAsync(db, otherEmployee.Id);

            var repo = new PerformanceRepository(db);
            var authRepo = new AuthRepository(db);
            var employeeRepo = new EmployeeRepository(db);
            var createHandler = new CreateGoalCommandHandler(repo, authRepo, employeeRepo, new RecordingAuditLogger(), NullLogger<CreateGoalCommandHandler>.Instance);
            var progressHandler = new UpdateGoalProgressCommandHandler(repo, authRepo, employeeRepo, new RecordingAuditLogger(), NullLogger<UpdateGoalProgressCommandHandler>.Instance);

            var goalId = await createHandler.Handle(new CreateGoalCommand
            {
                EmployeeId = employee.Id,
                Title = "Improve test coverage",
                StartDate = new DateTime(2026, 1, 1),
                TargetDate = new DateTime(2026, 3, 31),
                RequestingUserId = Guid.NewGuid(),
                IsPrivileged = true
            }, CancellationToken.None);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => progressHandler.Handle(new UpdateGoalProgressCommand
            {
                Id = goalId,
                ProgressPercent = 40,
                RequestingUserId = otherUser.Id,
                IsPrivileged = false,
                IsManager = false
            }, CancellationToken.None));
        }

        [Fact]
        public async Task Goal_SoftDeleteThenRestore_ExcludesThenReincludesFromQueries()
        {
            using var db = CreateDb();
            var employee = await SeedEmployeeAsync(db);
            var repo = new PerformanceRepository(db);
            var authRepo = new AuthRepository(db);
            var employeeRepo = new EmployeeRepository(db);
            var createHandler = new CreateGoalCommandHandler(repo, authRepo, employeeRepo, new RecordingAuditLogger(), NullLogger<CreateGoalCommandHandler>.Instance);
            var deleteHandler = new DeleteGoalCommandHandler(repo, authRepo, employeeRepo, new RecordingAuditLogger(), NullLogger<DeleteGoalCommandHandler>.Instance);
            var restoreHandler = new RestoreGoalCommandHandler(repo, authRepo, employeeRepo, new RecordingAuditLogger(), NullLogger<RestoreGoalCommandHandler>.Instance);

            var goalId = await createHandler.Handle(new CreateGoalCommand
            {
                EmployeeId = employee.Id,
                Title = "Goal to delete",
                StartDate = new DateTime(2026, 1, 1),
                TargetDate = new DateTime(2026, 3, 31),
                RequestingUserId = Guid.NewGuid(),
                IsPrivileged = true
            }, CancellationToken.None);

            await deleteHandler.Handle(new DeleteGoalCommand { Id = goalId, RequestingUserId = Guid.NewGuid(), IsPrivileged = true }, CancellationToken.None);
            Assert.Null(await repo.GetGoalByIdAsync(goalId, CancellationToken.None));

            await restoreHandler.Handle(new RestoreGoalCommand { Id = goalId, RequestingUserId = Guid.NewGuid(), IsPrivileged = true }, CancellationToken.None);
            var restored = await repo.GetGoalByIdAsync(goalId, CancellationToken.None);
            Assert.NotNull(restored);
            Assert.False(restored!.IsDeleted);
        }

        [Fact]
        public async Task AddGoalKpi_ThenUpdateProgress_PersistsCurrentValue()
        {
            using var db = CreateDb();
            var employee = await SeedEmployeeAsync(db);
            var employeeUser = await SeedUserAsync(db, employee.Id);

            var repo = new PerformanceRepository(db);
            var authRepo = new AuthRepository(db);
            var employeeRepo = new EmployeeRepository(db);
            var createHandler = new CreateGoalCommandHandler(repo, authRepo, employeeRepo, new RecordingAuditLogger(), NullLogger<CreateGoalCommandHandler>.Instance);
            var addKpiHandler = new AddGoalKpiCommandHandler(repo, authRepo, employeeRepo, new RecordingAuditLogger(), NullLogger<AddGoalKpiCommandHandler>.Instance);
            var kpiProgressHandler = new UpdateGoalKpiProgressCommandHandler(repo, authRepo, employeeRepo, new RecordingAuditLogger(), NullLogger<UpdateGoalKpiProgressCommandHandler>.Instance);

            var goalId = await createHandler.Handle(new CreateGoalCommand
            {
                EmployeeId = employee.Id,
                Title = "Increase sales",
                StartDate = new DateTime(2026, 1, 1),
                TargetDate = new DateTime(2026, 3, 31),
                RequestingUserId = Guid.NewGuid(),
                IsPrivileged = true
            }, CancellationToken.None);

            var kpiId = await addKpiHandler.Handle(new AddGoalKpiCommand
            {
                GoalId = goalId,
                Name = "New deals closed",
                TargetValue = 10,
                Unit = "deals",
                RequestingUserId = Guid.NewGuid(),
                IsPrivileged = true
            }, CancellationToken.None);

            await kpiProgressHandler.Handle(new UpdateGoalKpiProgressCommand
            {
                Id = kpiId,
                CurrentValue = 6,
                RequestingUserId = employeeUser.Id,
                IsPrivileged = false,
                IsManager = false
            }, CancellationToken.None);

            var goal = await repo.GetGoalByIdAsync(goalId, CancellationToken.None);
            var kpi = goal!.Kpis.Single(k => k.Id == kpiId);
            Assert.Equal(6, kpi.CurrentValue);
        }

        [Fact]
        public async Task GetGoals_PlainEmployee_IsScopedToSelfRegardlessOfFilter()
        {
            using var db = CreateDb();
            var selfEmployee = await SeedEmployeeAsync(db);
            var otherEmployee = await SeedEmployeeAsync(db);
            var selfUser = await SeedUserAsync(db, selfEmployee.Id);

            var repo = new PerformanceRepository(db);
            var authRepo = new AuthRepository(db);
            var employeeRepo = new EmployeeRepository(db);
            var createHandler = new CreateGoalCommandHandler(repo, authRepo, employeeRepo, new RecordingAuditLogger(), NullLogger<CreateGoalCommandHandler>.Instance);

            await createHandler.Handle(new CreateGoalCommand { EmployeeId = selfEmployee.Id, Title = "Self goal", StartDate = new DateTime(2026, 1, 1), TargetDate = new DateTime(2026, 3, 31), RequestingUserId = Guid.NewGuid(), IsPrivileged = true }, CancellationToken.None);
            await createHandler.Handle(new CreateGoalCommand { EmployeeId = otherEmployee.Id, Title = "Other's goal", StartDate = new DateTime(2026, 1, 1), TargetDate = new DateTime(2026, 3, 31), RequestingUserId = Guid.NewGuid(), IsPrivileged = true }, CancellationToken.None);

            var queryHandler = new GetGoalsQueryHandler(repo, authRepo, employeeRepo);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => queryHandler.Handle(new GetGoalsQuery
            {
                EmployeeId = otherEmployee.Id, // attempt to view someone else's goals
                RequestingUserId = selfUser.Id,
                IsPrivileged = false,
                IsManager = false
            }, CancellationToken.None));

            var ownResult = await queryHandler.Handle(new GetGoalsQuery
            {
                RequestingUserId = selfUser.Id,
                IsPrivileged = false,
                IsManager = false
            }, CancellationToken.None);

            Assert.Single(ownResult.Data);
            Assert.Equal(selfEmployee.Id, ownResult.Data.Single().EmployeeId);
        }

        // ─── Reviews ───────────────────────────────────────────────────────────────

        [Fact]
        public async Task CreateReview_ByManagerAssigningSomeoneElseAsReviewer_ThrowsUnauthorized()
        {
            using var db = CreateDb();
            var manager = await SeedEmployeeAsync(db);
            var report = await SeedEmployeeAsync(db, manager.Id);
            var otherReviewer = await SeedEmployeeAsync(db, manager.Id);
            var managerUser = await SeedUserAsync(db, manager.Id);

            var repo = new PerformanceRepository(db);
            var authRepo = new AuthRepository(db);
            var employeeRepo = new EmployeeRepository(db);
            var handler = new CreateReviewCommandHandler(repo, authRepo, employeeRepo, new RecordingAuditLogger(), NullLogger<CreateReviewCommandHandler>.Instance);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(new CreateReviewCommand
            {
                EmployeeId = report.Id,
                ReviewerEmployeeId = otherReviewer.Id, // not the caller themselves
                ReviewPeriodStart = new DateTime(2026, 1, 1),
                ReviewPeriodEnd = new DateTime(2026, 6, 30),
                RequestingUserId = managerUser.Id,
                IsPrivileged = false,
                IsManager = true
            }, CancellationToken.None));
        }

        [Fact]
        public async Task SubmitSelfAssessment_ByOwner_TransitionsToSelfAssessmentSubmitted()
        {
            using var db = CreateDb();
            var manager = await SeedEmployeeAsync(db);
            var report = await SeedEmployeeAsync(db, manager.Id);
            var managerUser = await SeedUserAsync(db, manager.Id);
            var reportUser = await SeedUserAsync(db, report.Id);

            var repo = new PerformanceRepository(db);
            var authRepo = new AuthRepository(db);
            var employeeRepo = new EmployeeRepository(db);
            var createHandler = new CreateReviewCommandHandler(repo, authRepo, employeeRepo, new RecordingAuditLogger(), NullLogger<CreateReviewCommandHandler>.Instance);
            var selfAssessmentHandler = new SubmitSelfAssessmentCommandHandler(repo, authRepo, new RecordingAuditLogger(), NullLogger<SubmitSelfAssessmentCommandHandler>.Instance);

            var reviewId = await createHandler.Handle(new CreateReviewCommand
            {
                EmployeeId = report.Id,
                ReviewerEmployeeId = manager.Id,
                ReviewPeriodStart = new DateTime(2026, 1, 1),
                ReviewPeriodEnd = new DateTime(2026, 6, 30),
                RequestingUserId = managerUser.Id,
                IsPrivileged = false,
                IsManager = true
            }, CancellationToken.None);

            await selfAssessmentHandler.Handle(new SubmitSelfAssessmentCommand
            {
                Id = reviewId,
                SelfAssessment = "I delivered the roadmap on time.",
                RequestingUserId = reportUser.Id,
                IsPrivileged = false
            }, CancellationToken.None);

            var review = await repo.GetReviewByIdAsync(reviewId, CancellationToken.None);
            Assert.Equal(ReviewStatus.SelfAssessmentSubmitted, review!.Status);
            Assert.NotNull(review.SelfSubmittedAtUtc);
        }

        [Fact]
        public async Task SubmitSelfAssessment_ByNonOwner_ThrowsUnauthorized()
        {
            using var db = CreateDb();
            var manager = await SeedEmployeeAsync(db);
            var report = await SeedEmployeeAsync(db, manager.Id);
            var managerUser = await SeedUserAsync(db, manager.Id);
            var strangerUser = await SeedUserAsync(db, (await SeedEmployeeAsync(db)).Id);

            var repo = new PerformanceRepository(db);
            var authRepo = new AuthRepository(db);
            var employeeRepo = new EmployeeRepository(db);
            var createHandler = new CreateReviewCommandHandler(repo, authRepo, employeeRepo, new RecordingAuditLogger(), NullLogger<CreateReviewCommandHandler>.Instance);
            var selfAssessmentHandler = new SubmitSelfAssessmentCommandHandler(repo, authRepo, new RecordingAuditLogger(), NullLogger<SubmitSelfAssessmentCommandHandler>.Instance);

            var reviewId = await createHandler.Handle(new CreateReviewCommand
            {
                EmployeeId = report.Id,
                ReviewerEmployeeId = manager.Id,
                ReviewPeriodStart = new DateTime(2026, 1, 1),
                ReviewPeriodEnd = new DateTime(2026, 6, 30),
                RequestingUserId = managerUser.Id,
                IsPrivileged = false,
                IsManager = true
            }, CancellationToken.None);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => selfAssessmentHandler.Handle(new SubmitSelfAssessmentCommand
            {
                Id = reviewId,
                SelfAssessment = "Trying to submit someone else's self-assessment.",
                RequestingUserId = strangerUser.Id,
                IsPrivileged = false
            }, CancellationToken.None));
        }

        [Fact]
        public async Task SubmitManagerReview_ByAssignedReviewer_CompletesReviewWithRating()
        {
            using var db = CreateDb();
            var manager = await SeedEmployeeAsync(db);
            var report = await SeedEmployeeAsync(db, manager.Id);
            var managerUser = await SeedUserAsync(db, manager.Id);

            var repo = new PerformanceRepository(db);
            var authRepo = new AuthRepository(db);
            var employeeRepo = new EmployeeRepository(db);
            var createHandler = new CreateReviewCommandHandler(repo, authRepo, employeeRepo, new RecordingAuditLogger(), NullLogger<CreateReviewCommandHandler>.Instance);
            var managerReviewHandler = new SubmitManagerReviewCommandHandler(repo, authRepo, new RecordingAuditLogger(), NullLogger<SubmitManagerReviewCommandHandler>.Instance);

            var reviewId = await createHandler.Handle(new CreateReviewCommand
            {
                EmployeeId = report.Id,
                ReviewerEmployeeId = manager.Id,
                ReviewPeriodStart = new DateTime(2026, 1, 1),
                ReviewPeriodEnd = new DateTime(2026, 6, 30),
                RequestingUserId = managerUser.Id,
                IsPrivileged = false,
                IsManager = true
            }, CancellationToken.None);

            await managerReviewHandler.Handle(new SubmitManagerReviewCommand
            {
                Id = reviewId,
                ManagerAssessment = "Strong quarter, exceeded targets.",
                OverallRating = 4.5m,
                RequestingUserId = managerUser.Id,
                IsPrivileged = false
            }, CancellationToken.None);

            var review = await repo.GetReviewByIdAsync(reviewId, CancellationToken.None);
            Assert.Equal(ReviewStatus.Completed, review!.Status);
            Assert.Equal(4.5m, review.OverallRating);
            Assert.NotNull(review.CompletedAtUtc);
        }

        [Fact]
        public async Task SubmitManagerReview_ByWrongReviewer_ThrowsUnauthorized()
        {
            using var db = CreateDb();
            var manager = await SeedEmployeeAsync(db);
            var report = await SeedEmployeeAsync(db, manager.Id);
            var managerUser = await SeedUserAsync(db, manager.Id);
            var strangerEmployee = await SeedEmployeeAsync(db);
            var strangerUser = await SeedUserAsync(db, strangerEmployee.Id);

            var repo = new PerformanceRepository(db);
            var authRepo = new AuthRepository(db);
            var employeeRepo = new EmployeeRepository(db);
            var createHandler = new CreateReviewCommandHandler(repo, authRepo, employeeRepo, new RecordingAuditLogger(), NullLogger<CreateReviewCommandHandler>.Instance);
            var managerReviewHandler = new SubmitManagerReviewCommandHandler(repo, authRepo, new RecordingAuditLogger(), NullLogger<SubmitManagerReviewCommandHandler>.Instance);

            var reviewId = await createHandler.Handle(new CreateReviewCommand
            {
                EmployeeId = report.Id,
                ReviewerEmployeeId = manager.Id,
                ReviewPeriodStart = new DateTime(2026, 1, 1),
                ReviewPeriodEnd = new DateTime(2026, 6, 30),
                RequestingUserId = managerUser.Id,
                IsPrivileged = false,
                IsManager = true
            }, CancellationToken.None);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => managerReviewHandler.Handle(new SubmitManagerReviewCommand
            {
                Id = reviewId,
                ManagerAssessment = "Impersonating the reviewer.",
                OverallRating = 3,
                RequestingUserId = strangerUser.Id,
                IsPrivileged = false
            }, CancellationToken.None));
        }

        // ─── Promotions ────────────────────────────────────────────────────────────

        [Fact]
        public async Task ProposePromotion_ThenApprove_UpdatesEmployeeDesignation()
        {
            using var db = CreateDb();
            var seniorDesignationId = await SeedDesignationAsync(db, "Senior Engineer");
            var manager = await SeedEmployeeAsync(db);
            var report = await SeedEmployeeAsync(db, manager.Id);
            var managerUser = await SeedUserAsync(db, manager.Id);
            var hrUser = await SeedUserAsync(db, Guid.NewGuid());

            var repo = new PerformanceRepository(db);
            var authRepo = new AuthRepository(db);
            var employeeRepo = new EmployeeRepository(db);
            var proposeHandler = new ProposePromotionCommandHandler(repo, authRepo, employeeRepo, new RecordingAuditLogger(), NullLogger<ProposePromotionCommandHandler>.Instance);
            var approveHandler = new ApprovePromotionCommandHandler(repo, employeeRepo, new RecordingAuditLogger(), NullLogger<ApprovePromotionCommandHandler>.Instance);

            var promotionId = await proposeHandler.Handle(new ProposePromotionCommand
            {
                EmployeeId = report.Id,
                ToDesignationId = seniorDesignationId,
                EffectiveDate = new DateTime(2026, 4, 1),
                Reason = "Consistently exceeding expectations.",
                RequestingUserId = managerUser.Id,
                IsPrivileged = false,
                IsManager = true
            }, CancellationToken.None);

            await approveHandler.Handle(new ApprovePromotionCommand
            {
                Id = promotionId,
                RequestingUserId = hrUser.Id
            }, CancellationToken.None);

            var promotion = await repo.GetPromotionByIdAsync(promotionId, CancellationToken.None);
            Assert.Equal(PromotionStatus.Approved, promotion!.Status);

            var updatedEmployee = await employeeRepo.GetByIdAsync(report.Id, CancellationToken.None);
            Assert.Equal(seniorDesignationId, updatedEmployee!.DesignationId);
        }

        [Fact]
        public async Task ProposePromotion_SameDesignationAndDepartment_Throws()
        {
            using var db = CreateDb();
            var manager = await SeedEmployeeAsync(db);
            var report = await SeedEmployeeAsync(db, manager.Id);
            var managerUser = await SeedUserAsync(db, manager.Id);

            var repo = new PerformanceRepository(db);
            var authRepo = new AuthRepository(db);
            var employeeRepo = new EmployeeRepository(db);
            var proposeHandler = new ProposePromotionCommandHandler(repo, authRepo, employeeRepo, new RecordingAuditLogger(), NullLogger<ProposePromotionCommandHandler>.Instance);

            await Assert.ThrowsAsync<InvalidOperationException>(() => proposeHandler.Handle(new ProposePromotionCommand
            {
                EmployeeId = report.Id,
                ToDesignationId = report.DesignationId, // unchanged
                EffectiveDate = new DateTime(2026, 4, 1),
                Reason = "No real change.",
                RequestingUserId = managerUser.Id,
                IsPrivileged = false,
                IsManager = true
            }, CancellationToken.None));
        }

        [Fact]
        public async Task RejectPromotion_SetsRejectedStatusAndDoesNotChangeEmployee()
        {
            using var db = CreateDb();
            var seniorDesignationId = await SeedDesignationAsync(db, "Senior Engineer");
            var manager = await SeedEmployeeAsync(db);
            var report = await SeedEmployeeAsync(db, manager.Id);
            var managerUser = await SeedUserAsync(db, manager.Id);
            var hrUser = await SeedUserAsync(db, Guid.NewGuid());
            var originalDesignationId = report.DesignationId;

            var repo = new PerformanceRepository(db);
            var authRepo = new AuthRepository(db);
            var employeeRepo = new EmployeeRepository(db);
            var proposeHandler = new ProposePromotionCommandHandler(repo, authRepo, employeeRepo, new RecordingAuditLogger(), NullLogger<ProposePromotionCommandHandler>.Instance);
            var rejectHandler = new RejectPromotionCommandHandler(repo, new RecordingAuditLogger(), NullLogger<RejectPromotionCommandHandler>.Instance);

            var promotionId = await proposeHandler.Handle(new ProposePromotionCommand
            {
                EmployeeId = report.Id,
                ToDesignationId = seniorDesignationId,
                EffectiveDate = new DateTime(2026, 4, 1),
                Reason = "Proposed but will be rejected.",
                RequestingUserId = managerUser.Id,
                IsPrivileged = false,
                IsManager = true
            }, CancellationToken.None);

            await rejectHandler.Handle(new RejectPromotionCommand { Id = promotionId, DecisionNotes = "Not this cycle.", RequestingUserId = hrUser.Id }, CancellationToken.None);

            var promotion = await repo.GetPromotionByIdAsync(promotionId, CancellationToken.None);
            Assert.Equal(PromotionStatus.Rejected, promotion!.Status);

            var employee = await employeeRepo.GetByIdAsync(report.Id, CancellationToken.None);
            Assert.Equal(originalDesignationId, employee!.DesignationId);
        }

        [Fact]
        public async Task WithdrawPromotion_ByNonProposer_NonPrivileged_ThrowsUnauthorized()
        {
            using var db = CreateDb();
            var seniorDesignationId = await SeedDesignationAsync(db, "Senior Engineer");
            var manager = await SeedEmployeeAsync(db);
            var report = await SeedEmployeeAsync(db, manager.Id);
            var managerUser = await SeedUserAsync(db, manager.Id);

            var repo = new PerformanceRepository(db);
            var authRepo = new AuthRepository(db);
            var employeeRepo = new EmployeeRepository(db);
            var proposeHandler = new ProposePromotionCommandHandler(repo, authRepo, employeeRepo, new RecordingAuditLogger(), NullLogger<ProposePromotionCommandHandler>.Instance);
            var withdrawHandler = new WithdrawPromotionCommandHandler(repo, new RecordingAuditLogger(), NullLogger<WithdrawPromotionCommandHandler>.Instance);

            var promotionId = await proposeHandler.Handle(new ProposePromotionCommand
            {
                EmployeeId = report.Id,
                ToDesignationId = seniorDesignationId,
                EffectiveDate = new DateTime(2026, 4, 1),
                Reason = "Will attempt an unauthorized withdrawal.",
                RequestingUserId = managerUser.Id,
                IsPrivileged = false,
                IsManager = true
            }, CancellationToken.None);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => withdrawHandler.Handle(new WithdrawPromotionCommand
            {
                Id = promotionId,
                RequestingUserId = Guid.NewGuid(), // not the proposer
                IsPrivileged = false
            }, CancellationToken.None));
        }

        [Fact]
        public async Task FullPromotionLifecycle_ProposeApprove_ReflectsOnEmployee()
        {
            using var db = CreateDb();
            var leadDesignationId = await SeedDesignationAsync(db, "Lead Engineer");
            var manager = await SeedEmployeeAsync(db);
            var report = await SeedEmployeeAsync(db, manager.Id);
            var managerUser = await SeedUserAsync(db, manager.Id);
            var hrUser = await SeedUserAsync(db, Guid.NewGuid());

            var repo = new PerformanceRepository(db);
            var authRepo = new AuthRepository(db);
            var employeeRepo = new EmployeeRepository(db);
            var proposeHandler = new ProposePromotionCommandHandler(repo, authRepo, employeeRepo, new RecordingAuditLogger(), NullLogger<ProposePromotionCommandHandler>.Instance);
            var approveHandler = new ApprovePromotionCommandHandler(repo, employeeRepo, new RecordingAuditLogger(), NullLogger<ApprovePromotionCommandHandler>.Instance);
            var deleteHandler = new DeletePromotionCommandHandler(repo, new RecordingAuditLogger(), NullLogger<DeletePromotionCommandHandler>.Instance);
            var restoreHandler = new RestorePromotionCommandHandler(repo, new RecordingAuditLogger(), NullLogger<RestorePromotionCommandHandler>.Instance);

            var promotionId = await proposeHandler.Handle(new ProposePromotionCommand
            {
                EmployeeId = report.Id,
                ToDesignationId = leadDesignationId,
                EffectiveDate = new DateTime(2026, 4, 1),
                Reason = "Full lifecycle test.",
                RequestingUserId = managerUser.Id,
                IsPrivileged = false,
                IsManager = true
            }, CancellationToken.None);

            await approveHandler.Handle(new ApprovePromotionCommand { Id = promotionId, RequestingUserId = hrUser.Id }, CancellationToken.None);

            await deleteHandler.Handle(new DeletePromotionCommand { Id = promotionId, RequestingUserId = hrUser.Id }, CancellationToken.None);
            Assert.Null(await repo.GetPromotionByIdAsync(promotionId, CancellationToken.None));

            await restoreHandler.Handle(new RestorePromotionCommand { Id = promotionId, RequestingUserId = hrUser.Id }, CancellationToken.None);
            var restored = await repo.GetPromotionByIdAsync(promotionId, CancellationToken.None);
            Assert.NotNull(restored);
            Assert.Equal(PromotionStatus.Approved, restored!.Status);

            var employee = await employeeRepo.GetByIdAsync(report.Id, CancellationToken.None);
            Assert.Equal(leadDesignationId, employee!.DesignationId);
        }
    }
}
