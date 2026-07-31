using EMS.Application.Features.Recruitment.Commands;
using EMS.Application.Features.Recruitment.Handlers;
using EMS.Application.Features.Recruitment.Validators;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using EMS.Persistence.Context;
using EMS.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EMS.Tests
{
    public class RecruitmentTests
    {
        private class RecordingAuditLogger : IAuditLogger
        {
            public Task LogAsync(string entityName, Guid? entityId, string action, object? oldValues = null, object? newValues = null, CancellationToken ct = default)
                => Task.CompletedTask;
        }

        private static readonly Guid TestCompanyId = Guid.NewGuid();

        private class FakeCurrentUserService : ICurrentUserService
        {
            public Guid? UserId => Guid.NewGuid();
            public Guid? CompanyId => TestCompanyId;
            public string? IpAddress => null;
            public string? UserAgent => null;
        }

        private static ApplicationDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("ems_recruitment_test_" + Guid.NewGuid())
                .Options;
            return new ApplicationDbContext(options);
        }

        private static async Task<(Designation Designation, Department Department, OfficeLocation OfficeLocation)> SeedOrgDataAsync(ApplicationDbContext db)
        {
            var designation = new Designation { Id = Guid.NewGuid(), Name = "Software Engineer", Code = "SE-" + Guid.NewGuid().ToString("N")[..6], CreatedAtUtc = DateTime.UtcNow };
            var department = new Department { Id = Guid.NewGuid(), Name = "Engineering", CreatedAtUtc = DateTime.UtcNow };
            var officeLocation = new OfficeLocation { Id = Guid.NewGuid(), Name = "HQ", Code = "HQ-" + Guid.NewGuid().ToString("N")[..6], City = "City", Country = "Country", TimeZoneId = "UTC", CreatedAtUtc = DateTime.UtcNow };
            db.Designations.Add(designation);
            db.Departments.Add(department);
            db.OfficeLocations.Add(officeLocation);
            await db.SaveChangesAsync();
            return (designation, department, officeLocation);
        }

        private static async Task<(Employee Employee, User User)> SeedInterviewerAsync(ApplicationDbContext db, Guid designationId, Guid officeLocationId)
        {
            var employeeId = Guid.NewGuid();
            var employee = new Employee
            {
                Id = employeeId,
                EmployeeCode = "EMP-" + employeeId.ToString("N")[..8],
                FirstName = "Interviewer",
                LastName = "One",
                JoinDate = DateTime.UtcNow.Date,
                IsActive = true,
                DesignationId = designationId,
                OfficeLocationId = officeLocationId
            };
            db.Employees.Add(employee);

            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = "interviewer_" + Guid.NewGuid().ToString("N")[..8],
                Email = Guid.NewGuid() + "@test.local",
                PasswordHash = "hash",
                EmployeeId = employeeId
            };
            db.Users.Add(user);

            await db.SaveChangesAsync();
            return (employee, user);
        }

        private static CreateCandidateCommand ValidCreateCommand(Guid designationId) => new()
        {
            FirstName = "Jane",
            LastName = "Doe",
            Email = $"{Guid.NewGuid():N}@candidates.local",
            DesignationId = designationId,
            AppliedDate = DateTime.UtcNow.Date
        };

        [Fact]
        public async Task CreateCandidate_PersistsWithGeneratedNumberAndAppliedStatus()
        {
            using var db = CreateDb();
            var (designation, _, _) = await SeedOrgDataAsync(db);
            var repo = new RecruitmentRepository(db);
            var handler = new CreateCandidateCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<CreateCandidateCommandHandler>.Instance);

            var id = await handler.Handle(ValidCreateCommand(designation.Id), CancellationToken.None);

            var candidate = await repo.GetByIdAsync(id, CancellationToken.None);
            Assert.NotNull(candidate);
            Assert.StartsWith("CAN-", candidate!.CandidateNumber);
            Assert.Equal(CandidateStatus.Applied, candidate.Status);
        }

        [Fact]
        public async Task CreateCandidateCommandValidator_RejectsNonexistentDesignation()
        {
            var designationRepo = new Mock<IDesignationRepository>();
            designationRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Designation?)null);
            var departmentRepo = new Mock<IDepartmentRepository>();

            var validator = new CreateCandidateCommandValidator(designationRepo.Object, departmentRepo.Object, new FakeCurrentUserService());
            var result = await validator.ValidateAsync(new CreateCandidateCommand
            {
                FirstName = "Jane",
                LastName = "Doe",
                Email = "jane@candidates.local",
                DesignationId = Guid.NewGuid(),
                AppliedDate = DateTime.UtcNow
            });

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "DesignationId");
        }

        [Fact]
        public async Task RejectCandidate_IsTerminal_BlocksFurtherInterviewScheduling()
        {
            using var db = CreateDb();
            var (designation, _, _) = await SeedOrgDataAsync(db);
            var (interviewer, _) = await SeedInterviewerAsync(db, designation.Id, (await db.OfficeLocations.FirstAsync()).Id);
            var repo = new RecruitmentRepository(db);
            var createHandler = new CreateCandidateCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<CreateCandidateCommandHandler>.Instance);
            var rejectHandler = new RejectCandidateCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<RejectCandidateCommandHandler>.Instance);
            var scheduleHandler = new ScheduleInterviewCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<ScheduleInterviewCommandHandler>.Instance);

            var candidateId = await createHandler.Handle(ValidCreateCommand(designation.Id), CancellationToken.None);
            await rejectHandler.Handle(new RejectCandidateCommand { Id = candidateId, Reason = "Not a fit" }, CancellationToken.None);

            var candidate = await repo.GetByIdAsync(candidateId, CancellationToken.None);
            Assert.Equal(CandidateStatus.Rejected, candidate!.Status);

            await Assert.ThrowsAsync<InvalidOperationException>(() => scheduleHandler.Handle(new ScheduleInterviewCommand
            {
                CandidateId = candidateId,
                InterviewerEmployeeId = interviewer.Id,
                Round = "Technical Round 1",
                Mode = InterviewMode.VideoCall,
                ScheduledAtUtc = DateTime.UtcNow.AddDays(1)
            }, CancellationToken.None));
        }

        [Fact]
        public async Task ScheduleInterview_MovesCandidateToInterviewing()
        {
            using var db = CreateDb();
            var (designation, _, officeLocation) = await SeedOrgDataAsync(db);
            var (interviewer, _) = await SeedInterviewerAsync(db, designation.Id, officeLocation.Id);
            var repo = new RecruitmentRepository(db);
            var createHandler = new CreateCandidateCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<CreateCandidateCommandHandler>.Instance);
            var scheduleHandler = new ScheduleInterviewCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<ScheduleInterviewCommandHandler>.Instance);

            var candidateId = await createHandler.Handle(ValidCreateCommand(designation.Id), CancellationToken.None);
            var interviewId = await scheduleHandler.Handle(new ScheduleInterviewCommand
            {
                CandidateId = candidateId,
                InterviewerEmployeeId = interviewer.Id,
                Round = "Technical Round 1",
                Mode = InterviewMode.VideoCall,
                ScheduledAtUtc = DateTime.UtcNow.AddDays(1)
            }, CancellationToken.None);

            var candidate = await repo.GetByIdAsync(candidateId, CancellationToken.None);
            Assert.Equal(CandidateStatus.Interviewing, candidate!.Status);

            var interview = await repo.GetInterviewByIdAsync(interviewId, CancellationToken.None);
            Assert.Equal(InterviewStatus.Scheduled, interview!.Status);
        }

        [Fact]
        public async Task SubmitInterviewFeedback_AssignedInterviewerCanSubmit_OthersAreRejected()
        {
            using var db = CreateDb();
            var (designation, _, officeLocation) = await SeedOrgDataAsync(db);
            var (interviewer, interviewerUser) = await SeedInterviewerAsync(db, designation.Id, officeLocation.Id);
            var (otherEmployee, otherUser) = await SeedInterviewerAsync(db, designation.Id, officeLocation.Id);
            var repo = new RecruitmentRepository(db);
            var authRepo = new AuthRepository(db);
            var createHandler = new CreateCandidateCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<CreateCandidateCommandHandler>.Instance);
            var scheduleHandler = new ScheduleInterviewCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<ScheduleInterviewCommandHandler>.Instance);
            var feedbackHandler = new SubmitInterviewFeedbackCommandHandler(repo, authRepo, new RecordingAuditLogger(), NullLogger<SubmitInterviewFeedbackCommandHandler>.Instance);

            var candidateId = await createHandler.Handle(ValidCreateCommand(designation.Id), CancellationToken.None);
            var interviewId = await scheduleHandler.Handle(new ScheduleInterviewCommand
            {
                CandidateId = candidateId,
                InterviewerEmployeeId = interviewer.Id,
                Round = "Technical Round 1",
                Mode = InterviewMode.VideoCall,
                ScheduledAtUtc = DateTime.UtcNow.AddDays(1)
            }, CancellationToken.None);

            // A different employee (not the assigned interviewer) cannot submit feedback.
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => feedbackHandler.Handle(new SubmitInterviewFeedbackCommand
            {
                Id = interviewId,
                Feedback = "Looks good",
                Rating = 4,
                Outcome = InterviewOutcome.Passed,
                RequestingUserId = otherUser.Id,
                IsPrivileged = false
            }, CancellationToken.None));

            // The assigned interviewer can.
            await feedbackHandler.Handle(new SubmitInterviewFeedbackCommand
            {
                Id = interviewId,
                Feedback = "Strong candidate",
                Rating = 5,
                Outcome = InterviewOutcome.Passed,
                RequestingUserId = interviewerUser.Id,
                IsPrivileged = false
            }, CancellationToken.None);

            var interview = await repo.GetInterviewByIdAsync(interviewId, CancellationToken.None);
            Assert.Equal(InterviewStatus.Completed, interview!.Status);
            Assert.Equal(InterviewOutcome.Passed, interview.Outcome);
            Assert.Equal(5, interview.Rating);
        }

        [Fact]
        public async Task OfferLifecycle_SendThenAccept_SeedsDefaultOnboardingChecklist()
        {
            using var db = CreateDb();
            var (designation, department, _) = await SeedOrgDataAsync(db);
            var repo = new RecruitmentRepository(db);
            var createHandler = new CreateCandidateCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<CreateCandidateCommandHandler>.Instance);
            var createOfferHandler = new CreateOfferCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<CreateOfferCommandHandler>.Instance);
            var sendHandler = new SendOfferCommandHandler(repo, new FakePdfService(), new FakeFileStorageService(), new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<SendOfferCommandHandler>.Instance);
            var acceptHandler = new AcceptOfferCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<AcceptOfferCommandHandler>.Instance);

            var candidateId = await createHandler.Handle(ValidCreateCommand(designation.Id), CancellationToken.None);
            var offerId = await createOfferHandler.Handle(new CreateOfferCommand
            {
                CandidateId = candidateId,
                DesignationId = designation.Id,
                DepartmentId = department.Id,
                OfferedSalary = 5000m,
                JoiningDate = DateTime.UtcNow.Date.AddDays(30)
            }, CancellationToken.None);

            await sendHandler.Handle(new SendOfferCommand { Id = offerId }, CancellationToken.None);

            var candidateAfterSend = await repo.GetByIdAsync(candidateId, CancellationToken.None);
            Assert.Equal(CandidateStatus.Offered, candidateAfterSend!.Status);

            var offerAfterSend = await repo.GetOfferByIdAsync(offerId, CancellationToken.None);
            Assert.Equal(OfferStatus.Sent, offerAfterSend!.Status);
            Assert.NotNull(offerAfterSend.BlobPath);

            await acceptHandler.Handle(new AcceptOfferCommand { Id = offerId }, CancellationToken.None);

            var offerAfterAccept = await repo.GetOfferByIdAsync(offerId, CancellationToken.None);
            Assert.Equal(OfferStatus.Accepted, offerAfterAccept!.Status);

            var checklist = await repo.GetChecklistItemsAsync(candidateId, CancellationToken.None);
            Assert.True(checklist.Count() >= 5);
            Assert.All(checklist, item => Assert.False(item.IsCompleted));
        }

        [Fact]
        public async Task SendOffer_FromNonDraftStatus_Throws()
        {
            using var db = CreateDb();
            var (designation, department, _) = await SeedOrgDataAsync(db);
            var repo = new RecruitmentRepository(db);
            var createHandler = new CreateCandidateCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<CreateCandidateCommandHandler>.Instance);
            var createOfferHandler = new CreateOfferCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<CreateOfferCommandHandler>.Instance);
            var sendHandler = new SendOfferCommandHandler(repo, new FakePdfService(), new FakeFileStorageService(), new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<SendOfferCommandHandler>.Instance);

            var candidateId = await createHandler.Handle(ValidCreateCommand(designation.Id), CancellationToken.None);
            var offerId = await createOfferHandler.Handle(new CreateOfferCommand
            {
                CandidateId = candidateId,
                DesignationId = designation.Id,
                DepartmentId = department.Id,
                OfferedSalary = 5000m,
                JoiningDate = DateTime.UtcNow.Date.AddDays(30)
            }, CancellationToken.None);

            await sendHandler.Handle(new SendOfferCommand { Id = offerId }, CancellationToken.None);

            // Already Sent — sending again must fail.
            await Assert.ThrowsAsync<InvalidOperationException>(() => sendHandler.Handle(new SendOfferCommand { Id = offerId }, CancellationToken.None));
        }

        [Fact]
        public async Task ConvertCandidateToEmployee_RequiresAcceptedOffer()
        {
            using var db = CreateDb();
            var (designation, _, officeLocation) = await SeedOrgDataAsync(db);
            var recruitmentRepo = new RecruitmentRepository(db);
            var employeeRepo = new EmployeeRepository(db);
            var createHandler = new CreateCandidateCommandHandler(recruitmentRepo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<CreateCandidateCommandHandler>.Instance);
            var convertHandler = new ConvertCandidateToEmployeeCommandHandler(recruitmentRepo, employeeRepo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<ConvertCandidateToEmployeeCommandHandler>.Instance);

            var candidateId = await createHandler.Handle(ValidCreateCommand(designation.Id), CancellationToken.None);

            await Assert.ThrowsAsync<InvalidOperationException>(() => convertHandler.Handle(new ConvertCandidateToEmployeeCommand
            {
                CandidateId = candidateId,
                EmployeeCode = "EMP-NEW01",
                OfficeLocationId = officeLocation.Id
            }, CancellationToken.None));
        }

        [Fact]
        public async Task ConvertCandidateToEmployee_FullPipeline_CreatesEmployeeAndMarksCandidateHired()
        {
            using var db = CreateDb();
            var (designation, department, officeLocation) = await SeedOrgDataAsync(db);
            var recruitmentRepo = new RecruitmentRepository(db);
            var employeeRepo = new EmployeeRepository(db);
            var createHandler = new CreateCandidateCommandHandler(recruitmentRepo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<CreateCandidateCommandHandler>.Instance);
            var createOfferHandler = new CreateOfferCommandHandler(recruitmentRepo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<CreateOfferCommandHandler>.Instance);
            var sendHandler = new SendOfferCommandHandler(recruitmentRepo, new FakePdfService(), new FakeFileStorageService(), new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<SendOfferCommandHandler>.Instance);
            var acceptHandler = new AcceptOfferCommandHandler(recruitmentRepo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<AcceptOfferCommandHandler>.Instance);
            var convertHandler = new ConvertCandidateToEmployeeCommandHandler(recruitmentRepo, employeeRepo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<ConvertCandidateToEmployeeCommandHandler>.Instance);

            var candidateId = await createHandler.Handle(ValidCreateCommand(designation.Id), CancellationToken.None);
            var offerId = await createOfferHandler.Handle(new CreateOfferCommand
            {
                CandidateId = candidateId,
                DesignationId = designation.Id,
                DepartmentId = department.Id,
                OfferedSalary = 6000m,
                JoiningDate = DateTime.UtcNow.Date.AddDays(15)
            }, CancellationToken.None);
            await sendHandler.Handle(new SendOfferCommand { Id = offerId }, CancellationToken.None);
            await acceptHandler.Handle(new AcceptOfferCommand { Id = offerId }, CancellationToken.None);

            var employeeId = await convertHandler.Handle(new ConvertCandidateToEmployeeCommand
            {
                CandidateId = candidateId,
                EmployeeCode = "EMP-CONV01",
                OfficeLocationId = officeLocation.Id
            }, CancellationToken.None);

            var employee = await db.Employees.FindAsync(employeeId);
            Assert.NotNull(employee);
            Assert.Equal("EMP-CONV01", employee!.EmployeeCode);
            Assert.Equal(designation.Id, employee.DesignationId);
            Assert.Equal(department.Id, employee.DepartmentId);

            var candidate = await recruitmentRepo.GetByIdAsync(candidateId, CancellationToken.None);
            Assert.Equal(CandidateStatus.Hired, candidate!.Status);
            Assert.Equal(employeeId, candidate.ConvertedEmployeeId);

            // Converting a second time must fail — already hired.
            await Assert.ThrowsAsync<InvalidOperationException>(() => convertHandler.Handle(new ConvertCandidateToEmployeeCommand
            {
                CandidateId = candidateId,
                EmployeeCode = "EMP-CONV02",
                OfficeLocationId = officeLocation.Id
            }, CancellationToken.None));
        }

        [Fact]
        public async Task Candidate_SoftDeleteThenRestore_ExcludesThenReincludesFromQueries()
        {
            using var db = CreateDb();
            var (designation, _, _) = await SeedOrgDataAsync(db);
            var repo = new RecruitmentRepository(db);
            var createHandler = new CreateCandidateCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<CreateCandidateCommandHandler>.Instance);
            var deleteHandler = new DeleteCandidateCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<DeleteCandidateCommandHandler>.Instance);
            var restoreHandler = new RestoreCandidateCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<RestoreCandidateCommandHandler>.Instance);

            var candidateId = await createHandler.Handle(ValidCreateCommand(designation.Id), CancellationToken.None);
            await deleteHandler.Handle(new DeleteCandidateCommand { Id = candidateId }, CancellationToken.None);

            Assert.Null(await repo.GetByIdAsync(candidateId, CancellationToken.None));
            var deleted = await repo.GetByIdIncludingDeletedAsync(candidateId, CancellationToken.None);
            Assert.True(deleted!.IsDeleted);

            await restoreHandler.Handle(new RestoreCandidateCommand { Id = candidateId }, CancellationToken.None);
            var restored = await repo.GetByIdAsync(candidateId, CancellationToken.None);
            Assert.NotNull(restored);
            Assert.False(restored!.IsDeleted);
        }

        [Fact]
        public async Task CompleteChecklistItem_StampsCompletedAtAndCompletedBy()
        {
            using var db = CreateDb();
            var (designation, _, _) = await SeedOrgDataAsync(db);
            var repo = new RecruitmentRepository(db);
            var createHandler = new CreateCandidateCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<CreateCandidateCommandHandler>.Instance);
            var addItemHandler = new AddChecklistItemCommandHandler(repo, new FakeCurrentUserService(), NullLogger<AddChecklistItemCommandHandler>.Instance);
            var completeHandler = new CompleteChecklistItemCommandHandler(repo, NullLogger<CompleteChecklistItemCommandHandler>.Instance);

            var candidateId = await createHandler.Handle(ValidCreateCommand(designation.Id), CancellationToken.None);
            var itemId = await addItemHandler.Handle(new AddChecklistItemCommand { CandidateId = candidateId, ItemName = "Laptop Issued" }, CancellationToken.None);

            var completerId = Guid.NewGuid();
            await completeHandler.Handle(new CompleteChecklistItemCommand { Id = itemId, RequestingUserId = completerId }, CancellationToken.None);

            var item = await repo.GetChecklistItemByIdAsync(itemId, CancellationToken.None);
            Assert.True(item!.IsCompleted);
            Assert.NotNull(item.CompletedAtUtc);
            Assert.Equal(completerId, item.CompletedBy);
        }

        private class FakePdfService : IPdfService
        {
            public Task<byte[]> GeneratePayslipPdfAsync(PayslipDocument document) => Task.FromResult(new byte[] { 1 });
            public Task<byte[]> GenerateDashboardSummaryPdfAsync(EMS.Application.DTOs.DashboardSummaryDto summary, DateTime asOfDate, Guid? departmentId) => Task.FromResult(new byte[] { 1 });
            public Task<byte[]> GenerateOfferLetterPdfAsync(OfferLetterDocument document) => Task.FromResult(new byte[] { 1, 2, 3 });
        }

        private class FakeFileStorageService : IFileStorageService
        {
            public Task<string> SaveFileAsync(string container, string path, byte[] content, string contentType) => Task.FromResult(path);
            public Task<byte[]?> GetFileAsync(string container, string path) => Task.FromResult<byte[]?>(new byte[] { 1, 2, 3 });
            public Task DeleteFileAsync(string container, string path) => Task.CompletedTask;
        }
    }
}
