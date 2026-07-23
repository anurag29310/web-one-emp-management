using EMS.Application.Features.Attendance.Commands;
using EMS.Application.Features.Attendance.Handlers;
using EMS.Application.Features.Attendance.Queries;
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
    public class AttendanceTests
    {
        private static ApplicationDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("ems_attendance_test_" + Guid.NewGuid())
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

        private static async Task<Employee> SeedEmployeeAsync(ApplicationDbContext db, Guid? managerId = null)
        {
            var employee = new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeCode = "EMP" + Guid.NewGuid().ToString("N")[..6],
                FirstName = "Test",
                LastName = "Employee",
                JoinDate = DateTime.UtcNow.Date,
                ManagerId = managerId,
                IsActive = true
            };
            await db.Employees.AddAsync(employee);
            await db.SaveChangesAsync();
            return employee;
        }

        [Fact]
        public async Task CheckIn_ForOwnEmployeeId_CreatesPresentRecord()
        {
            using var db = CreateDb();
            var employeeId = Guid.NewGuid();
            var user = await SeedUserAsync(db, employeeId);

            var repo = new AttendanceRepository(db);
            var authRepo = new AuthRepository(db);
            var handler = new CheckInCommandHandler(repo, authRepo, NullLogger<CheckInCommandHandler>.Instance);

            var checkInTime = new DateTime(2026, 6, 12, 3, 45, 0, DateTimeKind.Utc);
            var result = await handler.Handle(new CheckInCommand
            {
                EmployeeId = employeeId,
                CheckInAtUtc = checkInTime,
                RequestingUserId = user.Id,
                IsPrivileged = false
            }, CancellationToken.None);

            Assert.Equal(employeeId, result.EmployeeId);
            Assert.Equal(AttendanceStatus.Present, result.Status);
            Assert.False(result.IsLateArrival);
        }

        [Fact]
        public async Task CheckIn_ForAnotherEmployeeId_NonPrivileged_ThrowsUnauthorized()
        {
            using var db = CreateDb();
            var user = await SeedUserAsync(db, Guid.NewGuid());

            var repo = new AttendanceRepository(db);
            var authRepo = new AuthRepository(db);
            var handler = new CheckInCommandHandler(repo, authRepo, NullLogger<CheckInCommandHandler>.Instance);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(new CheckInCommand
            {
                EmployeeId = Guid.NewGuid(),
                CheckInAtUtc = DateTime.UtcNow,
                RequestingUserId = user.Id,
                IsPrivileged = false
            }, CancellationToken.None));
        }

        [Fact]
        public async Task CheckIn_Twice_ThrowsConflict()
        {
            using var db = CreateDb();
            var employeeId = Guid.NewGuid();
            var user = await SeedUserAsync(db, employeeId);

            var repo = new AttendanceRepository(db);
            var authRepo = new AuthRepository(db);
            var handler = new CheckInCommandHandler(repo, authRepo, NullLogger<CheckInCommandHandler>.Instance);

            var cmd = new CheckInCommand
            {
                EmployeeId = employeeId,
                CheckInAtUtc = DateTime.UtcNow,
                RequestingUserId = user.Id,
                IsPrivileged = false
            };

            await handler.Handle(cmd, CancellationToken.None);
            await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(cmd, CancellationToken.None));
        }

        [Fact]
        public async Task CheckIn_AfterShiftGraceWindow_MarksLateArrival()
        {
            using var db = CreateDb();
            var employeeId = Guid.NewGuid();
            var user = await SeedUserAsync(db, employeeId);

            var shift = new Shift
            {
                Id = Guid.NewGuid(),
                Name = "Day Shift",
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(17, 0, 0),
                GraceMinutes = 10,
                CreatedAtUtc = DateTime.UtcNow
            };
            await db.Shifts.AddAsync(shift);

            var checkInDate = new DateTime(2026, 6, 12, 0, 0, 0, DateTimeKind.Utc);
            await db.EmployeeShifts.AddAsync(new EmployeeShift
            {
                Id = Guid.NewGuid(),
                EmployeeId = employeeId,
                ShiftId = shift.Id,
                EffectiveFrom = checkInDate,
                CreatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var repo = new AttendanceRepository(db);
            var authRepo = new AuthRepository(db);
            var handler = new CheckInCommandHandler(repo, authRepo, NullLogger<CheckInCommandHandler>.Instance);

            // 9:20 is past the 9:00 + 10 minute grace window.
            var result = await handler.Handle(new CheckInCommand
            {
                EmployeeId = employeeId,
                CheckInAtUtc = checkInDate.AddHours(9).AddMinutes(20),
                RequestingUserId = user.Id,
                IsPrivileged = false
            }, CancellationToken.None);

            Assert.True(result.IsLateArrival);
            Assert.Equal(AttendanceStatus.Late, result.Status);
        }

        [Fact]
        public async Task CheckOut_WithoutCheckIn_ThrowsNotFound()
        {
            using var db = CreateDb();
            var employeeId = Guid.NewGuid();
            var user = await SeedUserAsync(db, employeeId);

            var repo = new AttendanceRepository(db);
            var authRepo = new AuthRepository(db);
            var handler = new CheckOutCommandHandler(repo, authRepo, NullLogger<CheckOutCommandHandler>.Instance);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(new CheckOutCommand
            {
                EmployeeId = employeeId,
                CheckOutAtUtc = DateTime.UtcNow,
                RequestingUserId = user.Id,
                IsPrivileged = false
            }, CancellationToken.None));

            Assert.Contains("not found", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task CheckOut_AfterCheckIn_ComputesWorkMinutes()
        {
            using var db = CreateDb();
            var employeeId = Guid.NewGuid();
            var user = await SeedUserAsync(db, employeeId);

            var repo = new AttendanceRepository(db);
            var authRepo = new AuthRepository(db);
            var checkInHandler = new CheckInCommandHandler(repo, authRepo, NullLogger<CheckInCommandHandler>.Instance);
            var checkOutHandler = new CheckOutCommandHandler(repo, authRepo, NullLogger<CheckOutCommandHandler>.Instance);

            var checkInTime = new DateTime(2026, 6, 12, 9, 0, 0, DateTimeKind.Utc);
            await checkInHandler.Handle(new CheckInCommand
            {
                EmployeeId = employeeId,
                CheckInAtUtc = checkInTime,
                RequestingUserId = user.Id,
                IsPrivileged = false
            }, CancellationToken.None);

            var result = await checkOutHandler.Handle(new CheckOutCommand
            {
                EmployeeId = employeeId,
                CheckOutAtUtc = checkInTime.AddHours(8),
                RequestingUserId = user.Id,
                IsPrivileged = false
            }, CancellationToken.None);

            Assert.Equal(480, result.TotalWorkMinutes);
        }

        [Fact]
        public async Task CreateAttendanceRecord_DuplicateForSameDate_ThrowsConflict()
        {
            using var db = CreateDb();
            var employeeId = Guid.NewGuid();
            var repo = new AttendanceRepository(db);
            var handler = new CreateAttendanceRecordCommandHandler(repo, NullLogger<CreateAttendanceRecordCommandHandler>.Instance);

            var cmd = new CreateAttendanceRecordCommand
            {
                EmployeeId = employeeId,
                AttendanceDate = new DateTime(2026, 6, 12),
                Status = "Absent"
            };

            await handler.Handle(cmd, CancellationToken.None);
            await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(cmd, CancellationToken.None));
        }

        [Fact]
        public async Task GetAttendanceRecordById_ForAnotherEmployee_NonPrivileged_ReturnsNull()
        {
            using var db = CreateDb();
            var ownerEmployeeId = Guid.NewGuid();
            var otherUser = await SeedUserAsync(db, Guid.NewGuid());

            var record = new AttendanceRecord
            {
                Id = Guid.NewGuid(),
                EmployeeId = ownerEmployeeId,
                AttendanceDate = DateTime.UtcNow.Date,
                Status = AttendanceStatus.Present,
                CreatedAtUtc = DateTime.UtcNow
            };
            await db.AttendanceRecords.AddAsync(record);
            await db.SaveChangesAsync();

            var repo = new AttendanceRepository(db);
            var authRepo = new AuthRepository(db);
            var employeeRepo = new EmployeeRepository(db);
            var handler = new GetAttendanceRecordByIdQueryHandler(repo, authRepo, employeeRepo);

            var result = await handler.Handle(new GetAttendanceRecordByIdQuery
            {
                Id = record.Id,
                RequestingUserId = otherUser.Id,
                IsAdminOrHr = false,
                IsManager = false
            }, CancellationToken.None);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetAttendanceRecordById_ForDirectReport_ManagerCanView()
        {
            using var db = CreateDb();
            var manager = await SeedEmployeeAsync(db);
            var report = await SeedEmployeeAsync(db, manager.Id);
            var managerUser = await SeedUserAsync(db, manager.Id);

            var record = new AttendanceRecord
            {
                Id = Guid.NewGuid(),
                EmployeeId = report.Id,
                AttendanceDate = DateTime.UtcNow.Date,
                Status = AttendanceStatus.Present,
                CreatedAtUtc = DateTime.UtcNow
            };
            await db.AttendanceRecords.AddAsync(record);
            await db.SaveChangesAsync();

            var repo = new AttendanceRepository(db);
            var authRepo = new AuthRepository(db);
            var employeeRepo = new EmployeeRepository(db);
            var handler = new GetAttendanceRecordByIdQueryHandler(repo, authRepo, employeeRepo);

            var result = await handler.Handle(new GetAttendanceRecordByIdQuery
            {
                Id = record.Id,
                RequestingUserId = managerUser.Id,
                IsAdminOrHr = false,
                IsManager = true
            }, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(report.Id, result!.EmployeeId);
        }

        [Fact]
        public async Task GetAttendanceRecords_PlainEmployee_IsScopedToSelfRegardlessOfFilter()
        {
            using var db = CreateDb();
            var selfId = Guid.NewGuid();
            var otherId = Guid.NewGuid();
            var user = await SeedUserAsync(db, selfId);

            await db.AttendanceRecords.AddRangeAsync(
                new AttendanceRecord { Id = Guid.NewGuid(), EmployeeId = selfId, AttendanceDate = DateTime.UtcNow.Date, Status = AttendanceStatus.Present, CreatedAtUtc = DateTime.UtcNow },
                new AttendanceRecord { Id = Guid.NewGuid(), EmployeeId = otherId, AttendanceDate = DateTime.UtcNow.Date, Status = AttendanceStatus.Present, CreatedAtUtc = DateTime.UtcNow });
            await db.SaveChangesAsync();

            var repo = new AttendanceRepository(db);
            var authRepo = new AuthRepository(db);
            var employeeRepo = new EmployeeRepository(db);
            var handler = new GetAttendanceRecordsQueryHandler(repo, authRepo, employeeRepo);

            var result = await handler.Handle(new GetAttendanceRecordsQuery
            {
                EmployeeId = otherId, // attempt to view someone else's records
                RequestingUserId = user.Id,
                IsAdminOrHr = false,
                IsManager = false
            }, CancellationToken.None);

            Assert.Single(result.Data);
            Assert.Equal(selfId, result.Data.Single().EmployeeId);
        }

        [Fact]
        public async Task CreateAttendanceCorrection_ForAnotherEmployeesRecord_ThrowsUnauthorized()
        {
            using var db = CreateDb();
            var ownerEmployeeId = Guid.NewGuid();
            var requestingUser = await SeedUserAsync(db, Guid.NewGuid());

            var record = new AttendanceRecord
            {
                Id = Guid.NewGuid(),
                EmployeeId = ownerEmployeeId,
                AttendanceDate = DateTime.UtcNow.Date,
                Status = AttendanceStatus.Present,
                CreatedAtUtc = DateTime.UtcNow
            };
            await db.AttendanceRecords.AddAsync(record);
            await db.SaveChangesAsync();

            var repo = new AttendanceRepository(db);
            var authRepo = new AuthRepository(db);
            var handler = new CreateAttendanceCorrectionCommandHandler(repo, authRepo, NullLogger<CreateAttendanceCorrectionCommandHandler>.Instance);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(new CreateAttendanceCorrectionCommand
            {
                AttendanceRecordId = record.Id,
                RequestedCheckInAtUtc = DateTime.UtcNow,
                Reason = "Forgot to check in on time",
                RequestingUserId = requestingUser.Id
            }, CancellationToken.None));
        }

        [Fact]
        public async Task ApproveAttendanceCorrection_AppliesRequestedTimesAndBlocksSelfApproval()
        {
            using var db = CreateDb();
            var employeeId = Guid.NewGuid();
            var employeeUser = await SeedUserAsync(db, employeeId);
            var approverEmployeeId = Guid.NewGuid();
            var approverUser = await SeedUserAsync(db, approverEmployeeId);

            var record = new AttendanceRecord
            {
                Id = Guid.NewGuid(),
                EmployeeId = employeeId,
                AttendanceDate = DateTime.UtcNow.Date,
                Status = AttendanceStatus.Present,
                CreatedAtUtc = DateTime.UtcNow
            };
            await db.AttendanceRecords.AddAsync(record);

            var correction = new AttendanceCorrection
            {
                Id = Guid.NewGuid(),
                AttendanceRecordId = record.Id,
                RequestedByEmployeeId = employeeId,
                RequestedCheckInAtUtc = record.AttendanceDate.AddHours(9),
                RequestedCheckOutAtUtc = record.AttendanceDate.AddHours(17),
                Reason = "Forgot to check out",
                Status = CorrectionStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow
            };
            await db.AttendanceCorrections.AddAsync(correction);
            await db.SaveChangesAsync();

            var repo = new AttendanceRepository(db);
            var authRepo = new AuthRepository(db);
            var handler = new ApproveAttendanceCorrectionCommandHandler(repo, authRepo, NullLogger<ApproveAttendanceCorrectionCommandHandler>.Instance);

            // The employee cannot approve their own correction.
            await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(new ApproveAttendanceCorrectionCommand
            {
                Id = correction.Id,
                ApproverUserId = employeeUser.Id
            }, CancellationToken.None));

            await handler.Handle(new ApproveAttendanceCorrectionCommand
            {
                Id = correction.Id,
                ApproverUserId = approverUser.Id,
                Comments = "Confirmed with security logs."
            }, CancellationToken.None);

            var updatedCorrection = await db.AttendanceCorrections.FindAsync(correction.Id);
            Assert.Equal(CorrectionStatus.Approved, updatedCorrection!.Status);
            Assert.Equal(approverEmployeeId, updatedCorrection.ApprovedByEmployeeId);

            var updatedRecord = await db.AttendanceRecords.FindAsync(record.Id);
            Assert.Equal(480, updatedRecord!.TotalWorkMinutes);
        }

        [Fact]
        public async Task RejectAttendanceCorrection_SetsRejectedStatusAndLeavesRecordUnchanged()
        {
            using var db = CreateDb();
            var employeeId = Guid.NewGuid();
            var approverUser = await SeedUserAsync(db, Guid.NewGuid());

            var record = new AttendanceRecord
            {
                Id = Guid.NewGuid(),
                EmployeeId = employeeId,
                AttendanceDate = DateTime.UtcNow.Date,
                Status = AttendanceStatus.Absent,
                CreatedAtUtc = DateTime.UtcNow
            };
            await db.AttendanceRecords.AddAsync(record);

            var correction = new AttendanceCorrection
            {
                Id = Guid.NewGuid(),
                AttendanceRecordId = record.Id,
                RequestedByEmployeeId = employeeId,
                RequestedCheckInAtUtc = record.AttendanceDate.AddHours(9),
                Reason = "Was present, forgot to check in",
                Status = CorrectionStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow
            };
            await db.AttendanceCorrections.AddAsync(correction);
            await db.SaveChangesAsync();

            var repo = new AttendanceRepository(db);
            var authRepo = new AuthRepository(db);
            var handler = new RejectAttendanceCorrectionCommandHandler(repo, authRepo, NullLogger<RejectAttendanceCorrectionCommandHandler>.Instance);

            await handler.Handle(new RejectAttendanceCorrectionCommand
            {
                Id = correction.Id,
                ApproverUserId = approverUser.Id,
                Comments = "No supporting evidence."
            }, CancellationToken.None);

            var updatedCorrection = await db.AttendanceCorrections.FindAsync(correction.Id);
            Assert.Equal(CorrectionStatus.Rejected, updatedCorrection!.Status);

            var unchangedRecord = await db.AttendanceRecords.FindAsync(record.Id);
            Assert.Null(unchangedRecord!.CheckInAtUtc);
            Assert.Equal(AttendanceStatus.Absent, unchangedRecord.Status);
        }

        [Fact]
        public async Task Shift_CreateUpdateDelete_Works()
        {
            using var db = CreateDb();
            var repo = new AttendanceRepository(db);

            var createHandler = new CreateShiftCommandHandler(repo, NullLogger<CreateShiftCommandHandler>.Instance);
            var updateHandler = new UpdateShiftCommandHandler(repo, NullLogger<UpdateShiftCommandHandler>.Instance);
            var deleteHandler = new DeleteShiftCommandHandler(repo, NullLogger<DeleteShiftCommandHandler>.Instance);

            var created = await createHandler.Handle(new CreateShiftCommand
            {
                Name = "Morning Shift",
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(17, 0, 0),
                GraceMinutes = 15
            }, CancellationToken.None);

            var updated = await updateHandler.Handle(new UpdateShiftCommand
            {
                Id = created.Id,
                Name = "Morning Shift (Updated)",
                StartTime = new TimeSpan(9, 30, 0),
                EndTime = new TimeSpan(17, 30, 0),
                GraceMinutes = 10
            }, CancellationToken.None);
            Assert.Equal("Morning Shift (Updated)", updated.Name);

            await deleteHandler.Handle(new DeleteShiftCommand { Id = created.Id }, CancellationToken.None);
            Assert.Null(await repo.GetShiftByIdAsync(created.Id, CancellationToken.None));
        }

        [Fact]
        public async Task AssignEmployeeShift_ThenCheckIn_UsesAssignedShiftForLateComputation()
        {
            using var db = CreateDb();
            var employeeId = Guid.NewGuid();
            var user = await SeedUserAsync(db, employeeId);

            var attendanceRepo = new AttendanceRepository(db);
            var shiftHandler = new CreateShiftCommandHandler(attendanceRepo, NullLogger<CreateShiftCommandHandler>.Instance);
            var assignHandler = new AssignEmployeeShiftCommandHandler(attendanceRepo, NullLogger<AssignEmployeeShiftCommandHandler>.Instance);

            var shift = await shiftHandler.Handle(new CreateShiftCommand
            {
                Name = "Night Shift",
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(17, 0, 0),
                GraceMinutes = 5
            }, CancellationToken.None);

            var effectiveFrom = new DateTime(2026, 6, 1);
            await assignHandler.Handle(new AssignEmployeeShiftCommand
            {
                EmployeeId = employeeId,
                ShiftId = shift.Id,
                EffectiveFrom = effectiveFrom
            }, CancellationToken.None);

            var authRepo = new AuthRepository(db);
            var checkInHandler = new CheckInCommandHandler(attendanceRepo, authRepo, NullLogger<CheckInCommandHandler>.Instance);

            var result = await checkInHandler.Handle(new CheckInCommand
            {
                EmployeeId = employeeId,
                CheckInAtUtc = new DateTime(2026, 6, 12, 9, 2, 0), // within the 5-minute grace window
                RequestingUserId = user.Id,
                IsPrivileged = false
            }, CancellationToken.None);

            Assert.Equal(shift.Id, result.ShiftId);
            Assert.False(result.IsLateArrival);
        }
    }
}
