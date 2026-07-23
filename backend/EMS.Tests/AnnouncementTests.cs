using EMS.Application.Features.Announcements.Commands;
using EMS.Application.Features.Announcements.Handlers;
using EMS.Application.Features.Announcements.Queries;
using EMS.Application.Features.Announcements.Validators;
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
    public class AnnouncementTests
    {
        private static ApplicationDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("ems_announcements_test_" + Guid.NewGuid())
                .Options;
            return new ApplicationDbContext(options);
        }

        private static async Task<(Department dept, Employee emp, User user)> SeedUserAsync(ApplicationDbContext db, string role, Guid? departmentId = null)
        {
            var dept = departmentId == null ? new Department { Id = Guid.NewGuid(), Name = "Engineering-" + Guid.NewGuid(), CreatedAtUtc = DateTime.UtcNow } : null;
            if (dept != null) db.Departments.Add(dept);

            var emp = new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeCode = "EMP-" + Guid.NewGuid().ToString("N")[..8],
                FirstName = "Test",
                LastName = "User",
                JoinDate = DateTime.UtcNow,
                DepartmentId = departmentId ?? dept!.Id
            };
            db.Employees.Add(emp);

            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = "user-" + Guid.NewGuid(),
                Email = Guid.NewGuid() + "@test.local",
                PasswordHash = "x",
                EmployeeId = emp.Id,
                CreatedAtUtc = DateTime.UtcNow
            };
            db.Users.Add(user);

            await db.SaveChangesAsync();
            return (dept!, emp, user);
        }

        [Fact]
        public async Task CreateAnnouncement_AllAudience_VisibleToAnyUser()
        {
            using var db = CreateDb();
            var repo = new AnnouncementRepository(db);
            var createHandler = new CreateAnnouncementCommandHandler(repo, NullLogger<CreateAnnouncementCommandHandler>.Instance);
            var (_, _, user) = await SeedUserAsync(db, "Employee");

            await createHandler.Handle(new CreateAnnouncementCommand
            {
                Title = "Company Picnic",
                Message = "Join us Friday",
                AudienceType = "All",
                CreatedByUserId = Guid.NewGuid()
            }, CancellationToken.None);

            var queryHandler = new GetAnnouncementsQueryHandler(repo);
            var result = await queryHandler.Handle(new GetAnnouncementsQuery { UserId = user.Id, RoleName = "Employee" }, CancellationToken.None);

            Assert.Single(result);
            Assert.False(result.Single().IsReadByMe);
        }

        [Fact]
        public async Task CreateAnnouncement_DepartmentAudience_OnlyVisibleToMatchingDepartment()
        {
            using var db = CreateDb();
            var repo = new AnnouncementRepository(db);
            var createHandler = new CreateAnnouncementCommandHandler(repo, NullLogger<CreateAnnouncementCommandHandler>.Instance);
            var (engDept, _, engUser) = await SeedUserAsync(db, "Employee");
            var (_, _, otherUser) = await SeedUserAsync(db, "Employee");

            await createHandler.Handle(new CreateAnnouncementCommand
            {
                Title = "Engineering Offsite",
                Message = "Details inside",
                AudienceType = "Department",
                DepartmentId = engDept.Id,
                CreatedByUserId = Guid.NewGuid()
            }, CancellationToken.None);

            var queryHandler = new GetAnnouncementsQueryHandler(repo);

            var engResult = await queryHandler.Handle(new GetAnnouncementsQuery { UserId = engUser.Id, RoleName = "Employee" }, CancellationToken.None);
            var otherResult = await queryHandler.Handle(new GetAnnouncementsQuery { UserId = otherUser.Id, RoleName = "Employee" }, CancellationToken.None);

            Assert.Single(engResult);
            Assert.Empty(otherResult);
        }

        [Fact]
        public async Task CreateAnnouncement_RoleAudience_OnlyVisibleToMatchingRole()
        {
            using var db = CreateDb();
            var repo = new AnnouncementRepository(db);
            var createHandler = new CreateAnnouncementCommandHandler(repo, NullLogger<CreateAnnouncementCommandHandler>.Instance);
            var (_, _, hrUser) = await SeedUserAsync(db, "HR");
            var (_, _, employeeUser) = await SeedUserAsync(db, "Employee");

            await createHandler.Handle(new CreateAnnouncementCommand
            {
                Title = "HR Policy Update",
                Message = "New leave policy",
                AudienceType = "Role",
                TargetRole = "HR",
                CreatedByUserId = Guid.NewGuid()
            }, CancellationToken.None);

            var queryHandler = new GetAnnouncementsQueryHandler(repo);

            var hrResult = await queryHandler.Handle(new GetAnnouncementsQuery { UserId = hrUser.Id, RoleName = "HR" }, CancellationToken.None);
            var employeeResult = await queryHandler.Handle(new GetAnnouncementsQuery { UserId = employeeUser.Id, RoleName = "Employee" }, CancellationToken.None);

            Assert.Single(hrResult);
            Assert.Empty(employeeResult);
        }

        [Fact]
        public async Task ExpiredAnnouncement_ExcludedFromVisibleList()
        {
            using var db = CreateDb();
            var repo = new AnnouncementRepository(db);
            var createHandler = new CreateAnnouncementCommandHandler(repo, NullLogger<CreateAnnouncementCommandHandler>.Instance);
            var (_, _, user) = await SeedUserAsync(db, "Employee");

            await createHandler.Handle(new CreateAnnouncementCommand
            {
                Title = "Expired Notice",
                Message = "Old news",
                AudienceType = "All",
                CreatedByUserId = Guid.NewGuid(),
                ExpiresAtUtc = DateTime.UtcNow.AddDays(-1)
            }, CancellationToken.None);

            var queryHandler = new GetAnnouncementsQueryHandler(repo);
            var result = await queryHandler.Handle(new GetAnnouncementsQuery { UserId = user.Id, RoleName = "Employee" }, CancellationToken.None);

            Assert.Empty(result);
        }

        [Fact]
        public async Task MarkRead_SetsIsReadByMe_AndIsIdempotent()
        {
            using var db = CreateDb();
            var repo = new AnnouncementRepository(db);
            var createHandler = new CreateAnnouncementCommandHandler(repo, NullLogger<CreateAnnouncementCommandHandler>.Instance);
            var markReadHandler = new MarkAnnouncementReadCommandHandler(repo);
            var (_, _, user) = await SeedUserAsync(db, "Employee");

            var id = await createHandler.Handle(new CreateAnnouncementCommand
            {
                Title = "Read Me",
                Message = "Please read",
                AudienceType = "All",
                CreatedByUserId = Guid.NewGuid()
            }, CancellationToken.None);

            await markReadHandler.Handle(new MarkAnnouncementReadCommand { AnnouncementId = id, UserId = user.Id }, CancellationToken.None);
            await markReadHandler.Handle(new MarkAnnouncementReadCommand { AnnouncementId = id, UserId = user.Id }, CancellationToken.None);

            var queryHandler = new GetAnnouncementsQueryHandler(repo);
            var result = await queryHandler.Handle(new GetAnnouncementsQuery { UserId = user.Id, RoleName = "Employee" }, CancellationToken.None);

            Assert.True(result.Single().IsReadByMe);
            Assert.Equal(1, db.AnnouncementReads.Count());
        }

        [Fact]
        public async Task DeleteAnnouncement_RetractsFromVisibleList()
        {
            using var db = CreateDb();
            var repo = new AnnouncementRepository(db);
            var createHandler = new CreateAnnouncementCommandHandler(repo, NullLogger<CreateAnnouncementCommandHandler>.Instance);
            var deleteHandler = new DeleteAnnouncementCommandHandler(repo, NullLogger<DeleteAnnouncementCommandHandler>.Instance);
            var (_, _, user) = await SeedUserAsync(db, "Employee");

            var id = await createHandler.Handle(new CreateAnnouncementCommand
            {
                Title = "Retract Me",
                Message = "Will be retracted",
                AudienceType = "All",
                CreatedByUserId = Guid.NewGuid()
            }, CancellationToken.None);

            await deleteHandler.Handle(new DeleteAnnouncementCommand { AnnouncementId = id }, CancellationToken.None);

            var queryHandler = new GetAnnouncementsQueryHandler(repo);
            var result = await queryHandler.Handle(new GetAnnouncementsQuery { UserId = user.Id, RoleName = "Employee" }, CancellationToken.None);

            Assert.Empty(result);
        }

        [Fact]
        public async Task Validator_RejectsMissingDepartmentId_WhenAudienceIsDepartment()
        {
            using var db = CreateDb();
            var departmentRepo = new DepartmentRepository(db);
            var validator = new CreateAnnouncementCommandValidator(departmentRepo);

            var result = await validator.ValidateAsync(new CreateAnnouncementCommand
            {
                Title = "T",
                Message = "M",
                AudienceType = "Department"
            });

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "DepartmentId");
        }

        [Fact]
        public async Task Validator_RejectsMissingTargetRole_WhenAudienceIsRole()
        {
            using var db = CreateDb();
            var departmentRepo = new DepartmentRepository(db);
            var validator = new CreateAnnouncementCommandValidator(departmentRepo);

            var result = await validator.ValidateAsync(new CreateAnnouncementCommand
            {
                Title = "T",
                Message = "M",
                AudienceType = "Role"
            });

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "TargetRole");
        }

        [Fact]
        public async Task Validator_RejectsPastExpiresAtUtc()
        {
            using var db = CreateDb();
            var departmentRepo = new DepartmentRepository(db);
            var validator = new CreateAnnouncementCommandValidator(departmentRepo);

            var result = await validator.ValidateAsync(new CreateAnnouncementCommand
            {
                Title = "T",
                Message = "M",
                AudienceType = "All",
                ExpiresAtUtc = DateTime.UtcNow.AddDays(-1)
            });

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "ExpiresAtUtc");
        }
    }
}
