using EMS.Application.Features.Leave.Commands;
using EMS.Domain.Entities;
using EMS.Persistence.Context;
using EMS.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EMS.Tests
{
    public class LeaveTests
    {
        private static ApplicationDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("ems_leave_test_" + Guid.NewGuid())
                .Options;
            return new ApplicationDbContext(options);
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
            await db.Users.AddAsync(user);
            await db.SaveChangesAsync();
            return user;
        }

        [Fact]
        public async Task CreateLeaveRequest_ForOwnEmployeeId_Succeeds()
        {
            using var db = CreateDb();
            var employeeId = Guid.NewGuid();
            var user = await SeedUserAsync(db, employeeId);

            var leaveRepo = new LeaveRepository(db);
            var authRepo = new AuthRepository(db);
            var handler = new EMS.Application.Features.Leave.Handlers.CreateLeaveRequestCommandHandler(
                leaveRepo, authRepo, NullLogger<EMS.Application.Features.Leave.Handlers.CreateLeaveRequestCommandHandler>.Instance);

            var cmd = new CreateLeaveRequestCommand
            {
                EmployeeId = employeeId,
                LeaveTypeId = Guid.NewGuid(),
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddDays(1),
                TotalDays = 2,
                RequestingUserId = user.Id,
                IsPrivileged = false
            };

            var result = await handler.Handle(cmd, CancellationToken.None);
            Assert.Equal(employeeId, result.EmployeeId);
        }

        [Fact]
        public async Task CreateLeaveRequest_ForAnotherEmployeeId_ThrowsUnauthorized()
        {
            using var db = CreateDb();
            var user = await SeedUserAsync(db, Guid.NewGuid());

            var leaveRepo = new LeaveRepository(db);
            var authRepo = new AuthRepository(db);
            var handler = new EMS.Application.Features.Leave.Handlers.CreateLeaveRequestCommandHandler(
                leaveRepo, authRepo, NullLogger<EMS.Application.Features.Leave.Handlers.CreateLeaveRequestCommandHandler>.Instance);

            var cmd = new CreateLeaveRequestCommand
            {
                EmployeeId = Guid.NewGuid(), // not the requester's own employee id
                LeaveTypeId = Guid.NewGuid(),
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddDays(1),
                TotalDays = 2,
                RequestingUserId = user.Id,
                IsPrivileged = false
            };

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(cmd, CancellationToken.None));
        }

        [Fact]
        public async Task GetLeaveById_ForAnotherEmployeesRequest_ReturnsNull()
        {
            using var db = CreateDb();
            var ownerEmployeeId = Guid.NewGuid();
            var otherUser = await SeedUserAsync(db, Guid.NewGuid());

            var leaveRequest = new LeaveRequest
            {
                Id = Guid.NewGuid(),
                EmployeeId = ownerEmployeeId,
                LeaveTypeId = Guid.NewGuid(),
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date,
                TotalDays = 1,
                Status = Domain.Enums.LeaveStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow
            };
            await db.LeaveRequests.AddAsync(leaveRequest);
            await db.SaveChangesAsync();

            var leaveRepo = new LeaveRepository(db);
            var authRepo = new AuthRepository(db);
            var handler = new EMS.Application.Features.Leave.Handlers.GetLeaveByIdQueryHandler(leaveRepo, authRepo);

            var result = await handler.Handle(new EMS.Application.Features.Leave.Queries.GetLeaveByIdQuery
            {
                Id = leaveRequest.Id,
                RequestingUserId = otherUser.Id,
                IsPrivileged = false
            }, CancellationToken.None);

            Assert.Null(result);
        }

        [Fact]
        public async Task CancelLeaveRequest_ByOwner_Succeeds()
        {
            using var db = CreateDb();
            var employeeId = Guid.NewGuid();
            var user = await SeedUserAsync(db, employeeId);

            var leaveRequest = new LeaveRequest
            {
                Id = Guid.NewGuid(),
                EmployeeId = employeeId,
                LeaveTypeId = Guid.NewGuid(),
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date,
                TotalDays = 1,
                Status = Domain.Enums.LeaveStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow
            };
            await db.LeaveRequests.AddAsync(leaveRequest);
            await db.SaveChangesAsync();

            var leaveRepo = new LeaveRepository(db);
            var authRepo = new AuthRepository(db);
            var handler = new EMS.Application.Features.Leave.Handlers.CancelLeaveRequestCommandHandler(
                leaveRepo, authRepo, NullLogger<EMS.Application.Features.Leave.Handlers.CancelLeaveRequestCommandHandler>.Instance);

            await handler.Handle(new CancelLeaveRequestCommand
            {
                Id = leaveRequest.Id,
                RequestedByUserId = user.Id,
                IsPrivileged = false
            }, CancellationToken.None);

            var updated = await db.LeaveRequests.FindAsync(leaveRequest.Id);
            Assert.Equal(Domain.Enums.LeaveStatus.Cancelled, updated!.Status);
        }

        [Fact]
        public async Task ApproveLeaveRequest_DeductsBalanceAndBlocksSelfApproval()
        {
            using var db = CreateDb();
            var employeeId = Guid.NewGuid();
            var leaveTypeId = Guid.NewGuid();
            var year = DateTime.UtcNow.Year;

            var employeeUser = await SeedUserAsync(db, employeeId);
            var approverEmployeeId = Guid.NewGuid();
            var approverUser = await SeedUserAsync(db, approverEmployeeId);

            var balance = new LeaveBalance
            {
                Id = Guid.NewGuid(),
                EmployeeId = employeeId,
                LeaveTypeId = leaveTypeId,
                Year = year,
                OpeningBalance = 10,
                Accrued = 0,
                Used = 0,
                Adjusted = 0,
                Available = 10
            };
            await db.LeaveBalances.AddAsync(balance);

            var leaveRequest = new LeaveRequest
            {
                Id = Guid.NewGuid(),
                EmployeeId = employeeId,
                LeaveTypeId = leaveTypeId,
                StartDate = new DateTime(year, 6, 1),
                EndDate = new DateTime(year, 6, 2),
                TotalDays = 2,
                Status = Domain.Enums.LeaveStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow
            };
            await db.LeaveRequests.AddAsync(leaveRequest);
            await db.SaveChangesAsync();

            var leaveRepo = new LeaveRepository(db);
            var authRepo = new AuthRepository(db);
            var handler = new EMS.Application.Features.Leave.Handlers.ApproveLeaveCommandHandler(
                leaveRepo, authRepo, NullLogger<EMS.Application.Features.Leave.Handlers.ApproveLeaveCommandHandler>.Instance);

            // The employee cannot approve their own request.
            await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(new ApproveLeaveCommand
            {
                Id = leaveRequest.Id,
                ApproverId = employeeUser.Id
            }, CancellationToken.None));

            // A different approver can, and it deducts the balance.
            await handler.Handle(new ApproveLeaveCommand
            {
                Id = leaveRequest.Id,
                ApproverId = approverUser.Id
            }, CancellationToken.None);

            var updatedBalance = await db.LeaveBalances.FindAsync(balance.Id);
            Assert.Equal(2, updatedBalance!.Used);
            Assert.Equal(8, updatedBalance.Available);

            var updatedRequest = await db.LeaveRequests.FindAsync(leaveRequest.Id);
            Assert.Equal(Domain.Enums.LeaveStatus.Approved, updatedRequest!.Status);
            Assert.Equal(approverEmployeeId, updatedRequest.ApproverEmployeeId);
        }

        [Fact]
        public async Task LeaveType_CreateUpdateDelete_Works()
        {
            using var db = CreateDb();
            var leaveRepo = new LeaveRepository(db);

            var createHandler = new EMS.Application.Features.Leave.Handlers.CreateLeaveTypeCommandHandler(
                leaveRepo, NullLogger<EMS.Application.Features.Leave.Handlers.CreateLeaveTypeCommandHandler>.Instance);
            var updateHandler = new EMS.Application.Features.Leave.Handlers.UpdateLeaveTypeCommandHandler(
                leaveRepo, NullLogger<EMS.Application.Features.Leave.Handlers.UpdateLeaveTypeCommandHandler>.Instance);
            var deleteHandler = new EMS.Application.Features.Leave.Handlers.DeleteLeaveTypeCommandHandler(
                leaveRepo, NullLogger<EMS.Application.Features.Leave.Handlers.DeleteLeaveTypeCommandHandler>.Instance);

            var created = await createHandler.Handle(new CreateLeaveTypeCommand
            {
                Name = "Casual Leave",
                Code = "CL",
                IsPaid = true,
                RequiresApproval = true,
                AnnualEntitlementDays = 12
            }, CancellationToken.None);

            await Assert.ThrowsAsync<InvalidOperationException>(() => createHandler.Handle(new CreateLeaveTypeCommand
            {
                Name = "Casual Leave 2",
                Code = "CL"
            }, CancellationToken.None));

            var updated = await updateHandler.Handle(new UpdateLeaveTypeCommand
            {
                Id = created.Id,
                Name = "Casual Leave (Updated)",
                Code = "CL",
                IsPaid = true,
                RequiresApproval = false,
                AnnualEntitlementDays = 15
            }, CancellationToken.None);
            Assert.Equal("Casual Leave (Updated)", updated.Name);

            await deleteHandler.Handle(new DeleteLeaveTypeCommand { Id = created.Id }, CancellationToken.None);
            Assert.Null(await leaveRepo.GetLeaveTypeByIdAsync(created.Id, CancellationToken.None));
        }
    }
}
