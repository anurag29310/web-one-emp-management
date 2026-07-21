using EMS.Application.Features.Employees.Commands;
using EMS.Application.Features.Employees.Handlers;
using EMS.Application.Features.Employees.Queries;
using EMS.Application.Features.Employees.Validators;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Persistence.Context;
using EMS.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EMS.Tests
{
    public class EmployeeTests
    {
        private static ApplicationDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("ems_employee_test_" + Guid.NewGuid())
                .Options;
            return new ApplicationDbContext(options);
        }

        private class RecordingAuditLogger : IAuditLogger
        {
            public List<(string EntityName, Guid? EntityId, string Action)> Calls { get; } = new();

            public Task LogAsync(string entityName, Guid? entityId, string action, object? oldValues = null, object? newValues = null, CancellationToken ct = default)
            {
                Calls.Add((entityName, entityId, action));
                return Task.CompletedTask;
            }
        }

        private static Employee NewEmployee(string code, string firstName, string lastName, bool isActive = true, Guid? managerId = null, Guid? departmentId = null, string? status = "Active") => new()
        {
            Id = Guid.NewGuid(),
            EmployeeCode = code,
            FirstName = firstName,
            LastName = lastName,
            Email = $"{code.ToLowerInvariant()}@test.local",
            JoinDate = DateTime.UtcNow.Date,
            IsActive = isActive,
            ManagerId = managerId,
            DepartmentId = departmentId,
            EmploymentStatus = status
        };

        // ─── CreateEmployeeCommandHandler ──────────────────────────────────────────

        [Fact]
        public async Task CreateEmployee_PersistsAndReturnsEmployee()
        {
            using var db = CreateDb();
            var repo = new EmployeeRepository(db);
            var auditLogger = new RecordingAuditLogger();
            var handler = new CreateEmployeeCommandHandler(repo, auditLogger, NullLogger<CreateEmployeeCommandHandler>.Instance);

            var cmd = new CreateEmployeeCommand
            {
                EmployeeCode = "EMP001",
                FirstName = "Alice",
                LastName = "Johnson",
                Email = "alice.johnson@test.local",
                JoinDate = DateTime.UtcNow.Date
            };

            var created = await handler.Handle(cmd, CancellationToken.None);

            Assert.NotEqual(Guid.Empty, created.Id);
            Assert.True(created.IsActive);
            Assert.Equal("EMP001", db.Employees.Single().EmployeeCode);
        }

        [Fact]
        public async Task CreateEmployee_WritesAuditLogEntry()
        {
            using var db = CreateDb();
            var repo = new EmployeeRepository(db);
            var auditLogger = new RecordingAuditLogger();
            var handler = new CreateEmployeeCommandHandler(repo, auditLogger, NullLogger<CreateEmployeeCommandHandler>.Instance);

            var created = await handler.Handle(new CreateEmployeeCommand
            {
                EmployeeCode = "EMP002",
                FirstName = "Bob",
                LastName = "Smith",
                JoinDate = DateTime.UtcNow.Date
            }, CancellationToken.None);

            var call = Assert.Single(auditLogger.Calls);
            Assert.Equal("Employee", call.EntityName);
            Assert.Equal(created.Id, call.EntityId);
            Assert.Equal("Created", call.Action);
        }

        [Fact]
        public async Task CreateEmployee_DuplicateEmployeeCode_ThrowsInvalidOperationException()
        {
            using var db = CreateDb();
            var repo = new EmployeeRepository(db);
            var handler = new CreateEmployeeCommandHandler(repo, new RecordingAuditLogger(), NullLogger<CreateEmployeeCommandHandler>.Instance);

            await handler.Handle(new CreateEmployeeCommand { EmployeeCode = "DUP1", FirstName = "A", LastName = "One", JoinDate = DateTime.UtcNow }, CancellationToken.None);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                handler.Handle(new CreateEmployeeCommand { EmployeeCode = "DUP1", FirstName = "B", LastName = "Two", JoinDate = DateTime.UtcNow }, CancellationToken.None));
        }

        [Fact]
        public async Task CreateEmployee_DuplicateEmail_ThrowsInvalidOperationException()
        {
            using var db = CreateDb();
            var repo = new EmployeeRepository(db);
            var handler = new CreateEmployeeCommandHandler(repo, new RecordingAuditLogger(), NullLogger<CreateEmployeeCommandHandler>.Instance);

            await handler.Handle(new CreateEmployeeCommand { EmployeeCode = "E1", FirstName = "A", LastName = "One", Email = "dup@test.local", JoinDate = DateTime.UtcNow }, CancellationToken.None);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                handler.Handle(new CreateEmployeeCommand { EmployeeCode = "E2", FirstName = "B", LastName = "Two", Email = "dup@test.local", JoinDate = DateTime.UtcNow }, CancellationToken.None));
        }

        // ─── UpdateEmployeeCommandHandler ──────────────────────────────────────────

        [Fact]
        public async Task UpdateEmployee_ChangesFieldsAndWritesAuditEntry()
        {
            using var db = CreateDb();
            var repo = new EmployeeRepository(db);
            var auditLogger = new RecordingAuditLogger();
            var createHandler = new CreateEmployeeCommandHandler(repo, auditLogger, NullLogger<CreateEmployeeCommandHandler>.Instance);
            var updateHandler = new UpdateEmployeeCommandHandler(repo, auditLogger, NullLogger<UpdateEmployeeCommandHandler>.Instance);

            var created = await createHandler.Handle(new CreateEmployeeCommand { EmployeeCode = "UPD1", FirstName = "Carol", LastName = "Lee", JoinDate = DateTime.UtcNow }, CancellationToken.None);

            var updated = await updateHandler.Handle(new UpdateEmployeeCommand
            {
                Id = created.Id,
                EmployeeCode = "UPD1",
                FirstName = "Carol",
                LastName = "Lee-Martinez",
                JoinDate = created.JoinDate
            }, CancellationToken.None);

            Assert.Equal("Lee-Martinez", updated.LastName);
            Assert.Contains(auditLogger.Calls, c => c.Action == "Updated" && c.EntityId == created.Id);
        }

        [Fact]
        public async Task UpdateEmployee_NotFound_Throws()
        {
            using var db = CreateDb();
            var repo = new EmployeeRepository(db);
            var handler = new UpdateEmployeeCommandHandler(repo, new RecordingAuditLogger(), NullLogger<UpdateEmployeeCommandHandler>.Instance);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                handler.Handle(new UpdateEmployeeCommand { Id = Guid.NewGuid(), EmployeeCode = "X", FirstName = "X", LastName = "X", JoinDate = DateTime.UtcNow }, CancellationToken.None));
        }

        [Fact]
        public async Task UpdateEmployee_DuplicateEmailOnAnotherEmployee_Throws()
        {
            using var db = CreateDb();
            var repo = new EmployeeRepository(db);
            var auditLogger = new RecordingAuditLogger();
            var createHandler = new CreateEmployeeCommandHandler(repo, auditLogger, NullLogger<CreateEmployeeCommandHandler>.Instance);
            var updateHandler = new UpdateEmployeeCommandHandler(repo, auditLogger, NullLogger<UpdateEmployeeCommandHandler>.Instance);

            await createHandler.Handle(new CreateEmployeeCommand { EmployeeCode = "A1", FirstName = "A", LastName = "One", Email = "taken@test.local", JoinDate = DateTime.UtcNow }, CancellationToken.None);
            var second = await createHandler.Handle(new CreateEmployeeCommand { EmployeeCode = "A2", FirstName = "B", LastName = "Two", Email = "free@test.local", JoinDate = DateTime.UtcNow }, CancellationToken.None);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                updateHandler.Handle(new UpdateEmployeeCommand { Id = second.Id, EmployeeCode = "A2", FirstName = "B", LastName = "Two", Email = "taken@test.local", JoinDate = DateTime.UtcNow }, CancellationToken.None));
        }

        [Fact]
        public async Task UpdateEmployee_KeepingOwnEmailAndCode_Succeeds()
        {
            using var db = CreateDb();
            var repo = new EmployeeRepository(db);
            var auditLogger = new RecordingAuditLogger();
            var createHandler = new CreateEmployeeCommandHandler(repo, auditLogger, NullLogger<CreateEmployeeCommandHandler>.Instance);
            var updateHandler = new UpdateEmployeeCommandHandler(repo, auditLogger, NullLogger<UpdateEmployeeCommandHandler>.Instance);

            var created = await createHandler.Handle(new CreateEmployeeCommand { EmployeeCode = "SAME1", FirstName = "D", LastName = "One", Email = "same@test.local", JoinDate = DateTime.UtcNow }, CancellationToken.None);

            var updated = await updateHandler.Handle(new UpdateEmployeeCommand { Id = created.Id, EmployeeCode = "SAME1", FirstName = "D", LastName = "OneUpdated", Email = "same@test.local", JoinDate = created.JoinDate }, CancellationToken.None);

            Assert.Equal("OneUpdated", updated.LastName);
        }

        // ─── DeleteEmployeeCommandHandler ──────────────────────────────────────────

        [Fact]
        public async Task DeleteEmployee_DeactivatesAndWritesAuditEntry()
        {
            using var db = CreateDb();
            var repo = new EmployeeRepository(db);
            var auditLogger = new RecordingAuditLogger();
            var createHandler = new CreateEmployeeCommandHandler(repo, auditLogger, NullLogger<CreateEmployeeCommandHandler>.Instance);
            var deleteHandler = new DeleteEmployeeCommandHandler(repo, auditLogger, NullLogger<DeleteEmployeeCommandHandler>.Instance);

            var created = await createHandler.Handle(new CreateEmployeeCommand { EmployeeCode = "DEL1", FirstName = "E", LastName = "One", JoinDate = DateTime.UtcNow }, CancellationToken.None);

            await deleteHandler.Handle(new DeleteEmployeeCommand { Id = created.Id }, CancellationToken.None);

            Assert.Null(await repo.GetByIdAsync(created.Id, CancellationToken.None));
            var stillThere = await repo.GetByIdIncludingDeletedAsync(created.Id, CancellationToken.None);
            Assert.NotNull(stillThere);
            Assert.False(stillThere!.IsActive);
            Assert.Contains(auditLogger.Calls, c => c.Action == "Deleted" && c.EntityId == created.Id);
        }

        [Fact]
        public async Task DeleteEmployee_NotFound_Throws()
        {
            using var db = CreateDb();
            var repo = new EmployeeRepository(db);
            var handler = new DeleteEmployeeCommandHandler(repo, new RecordingAuditLogger(), NullLogger<DeleteEmployeeCommandHandler>.Instance);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                handler.Handle(new DeleteEmployeeCommand { Id = Guid.NewGuid() }, CancellationToken.None));
        }

        // ─── RestoreEmployeeCommandHandler ─────────────────────────────────────────

        [Fact]
        public async Task RestoreEmployee_AfterDelete_ReactivatesEmployee()
        {
            using var db = CreateDb();
            var repo = new EmployeeRepository(db);
            var auditLogger = new RecordingAuditLogger();
            var createHandler = new CreateEmployeeCommandHandler(repo, auditLogger, NullLogger<CreateEmployeeCommandHandler>.Instance);
            var deleteHandler = new DeleteEmployeeCommandHandler(repo, auditLogger, NullLogger<DeleteEmployeeCommandHandler>.Instance);
            var restoreHandler = new RestoreEmployeeCommandHandler(repo, auditLogger, NullLogger<RestoreEmployeeCommandHandler>.Instance);

            var created = await createHandler.Handle(new CreateEmployeeCommand { EmployeeCode = "RES1", FirstName = "F", LastName = "One", JoinDate = DateTime.UtcNow }, CancellationToken.None);
            await deleteHandler.Handle(new DeleteEmployeeCommand { Id = created.Id }, CancellationToken.None);

            await restoreHandler.Handle(new RestoreEmployeeCommand { Id = created.Id }, CancellationToken.None);

            var restored = await repo.GetByIdAsync(created.Id, CancellationToken.None);
            Assert.NotNull(restored);
            Assert.True(restored!.IsActive);
            Assert.Equal("Active", restored.EmploymentStatus);
            Assert.Null(restored.ExitDate);
            Assert.Contains(auditLogger.Calls, c => c.Action == "Restored" && c.EntityId == created.Id);
        }

        [Fact]
        public async Task RestoreEmployee_WhenNotDeleted_Throws()
        {
            using var db = CreateDb();
            var repo = new EmployeeRepository(db);
            var auditLogger = new RecordingAuditLogger();
            var createHandler = new CreateEmployeeCommandHandler(repo, auditLogger, NullLogger<CreateEmployeeCommandHandler>.Instance);
            var restoreHandler = new RestoreEmployeeCommandHandler(repo, auditLogger, NullLogger<RestoreEmployeeCommandHandler>.Instance);

            var created = await createHandler.Handle(new CreateEmployeeCommand { EmployeeCode = "RES2", FirstName = "G", LastName = "One", JoinDate = DateTime.UtcNow }, CancellationToken.None);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                restoreHandler.Handle(new RestoreEmployeeCommand { Id = created.Id }, CancellationToken.None));
        }

        [Fact]
        public async Task RestoreEmployee_NotFound_Throws()
        {
            using var db = CreateDb();
            var repo = new EmployeeRepository(db);
            var handler = new RestoreEmployeeCommandHandler(repo, new RecordingAuditLogger(), NullLogger<RestoreEmployeeCommandHandler>.Instance);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                handler.Handle(new RestoreEmployeeCommand { Id = Guid.NewGuid() }, CancellationToken.None));
        }

        // ─── Activate / Deactivate ──────────────────────────────────────────────────

        [Fact]
        public async Task ActivateEmployee_SetsIsActiveTrue()
        {
            using var db = CreateDb();
            await db.Employees.AddAsync(NewEmployee("ACT1", "H", "One", isActive: false));
            await db.SaveChangesAsync();
            var repo = new EmployeeRepository(db);
            var target = db.Employees.Single();
            var auditLogger = new RecordingAuditLogger();
            var handler = new ActivateEmployeeCommandHandler(repo, auditLogger, NullLogger<ActivateEmployeeCommandHandler>.Instance);

            await handler.Handle(new ActivateEmployeeCommand { Id = target.Id }, CancellationToken.None);

            Assert.True((await repo.GetByIdAsync(target.Id))!.IsActive);
            Assert.Contains(auditLogger.Calls, c => c.Action == "Activated" && c.EntityId == target.Id);
        }

        [Fact]
        public async Task ActivateEmployee_NotFound_Throws()
        {
            using var db = CreateDb();
            var repo = new EmployeeRepository(db);
            var handler = new ActivateEmployeeCommandHandler(repo, new RecordingAuditLogger(), NullLogger<ActivateEmployeeCommandHandler>.Instance);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                handler.Handle(new ActivateEmployeeCommand { Id = Guid.NewGuid() }, CancellationToken.None));
        }

        [Fact]
        public async Task DeactivateEmployee_SetsIsActiveFalse()
        {
            using var db = CreateDb();
            await db.Employees.AddAsync(NewEmployee("DEA1", "I", "One", isActive: true));
            await db.SaveChangesAsync();
            var repo = new EmployeeRepository(db);
            var target = db.Employees.Single();
            var auditLogger = new RecordingAuditLogger();
            var handler = new DeactivateEmployeeCommandHandler(repo, auditLogger, NullLogger<DeactivateEmployeeCommandHandler>.Instance);

            await handler.Handle(new DeactivateEmployeeCommand { Id = target.Id }, CancellationToken.None);

            Assert.Null(await repo.GetByIdAsync(target.Id));
            Assert.Contains(auditLogger.Calls, c => c.Action == "Deactivated" && c.EntityId == target.Id);
        }

        [Fact]
        public async Task DeactivateEmployee_NotFound_Throws()
        {
            using var db = CreateDb();
            var repo = new EmployeeRepository(db);
            var handler = new DeactivateEmployeeCommandHandler(repo, new RecordingAuditLogger(), NullLogger<DeactivateEmployeeCommandHandler>.Instance);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                handler.Handle(new DeactivateEmployeeCommand { Id = Guid.NewGuid() }, CancellationToken.None));
        }

        // ─── UpdateEmployeeStatusCommandHandler ────────────────────────────────────

        [Theory]
        [InlineData("Terminated", false)]
        [InlineData("Inactive", false)]
        [InlineData("Active", true)]
        [InlineData("OnLeave", true)]
        public async Task UpdateEmployeeStatus_TogglesIsActiveBasedOnStatus(string status, bool expectedIsActive)
        {
            using var db = CreateDb();
            await db.Employees.AddAsync(NewEmployee("STA1", "J", "One", isActive: true));
            await db.SaveChangesAsync();
            var repo = new EmployeeRepository(db);
            var target = db.Employees.Single();
            var auditLogger = new RecordingAuditLogger();
            var handler = new UpdateEmployeeStatusCommandHandler(repo, auditLogger, NullLogger<UpdateEmployeeStatusCommandHandler>.Instance);

            await handler.Handle(new UpdateEmployeeStatusCommand { Id = target.Id, Status = status }, CancellationToken.None);

            var updated = await repo.GetByIdIncludingDeletedAsync(target.Id);
            Assert.Equal(status, updated!.EmploymentStatus);
            Assert.Equal(expectedIsActive, updated.IsActive);
            Assert.Contains(auditLogger.Calls, c => c.Action == "StatusChanged" && c.EntityId == target.Id);
        }

        [Fact]
        public async Task UpdateEmployeeStatus_SetsExitDate()
        {
            using var db = CreateDb();
            await db.Employees.AddAsync(NewEmployee("STA2", "K", "One", isActive: true));
            await db.SaveChangesAsync();
            var repo = new EmployeeRepository(db);
            var target = db.Employees.Single();
            var handler = new UpdateEmployeeStatusCommandHandler(repo, new RecordingAuditLogger(), NullLogger<UpdateEmployeeStatusCommandHandler>.Instance);
            var exitDate = DateTime.UtcNow.Date;

            await handler.Handle(new UpdateEmployeeStatusCommand { Id = target.Id, Status = "Terminated", ExitDate = exitDate }, CancellationToken.None);

            var updated = await repo.GetByIdIncludingDeletedAsync(target.Id);
            Assert.Equal(exitDate, updated!.ExitDate);
        }

        [Fact]
        public async Task UpdateEmployeeStatus_NotFound_Throws()
        {
            using var db = CreateDb();
            var repo = new EmployeeRepository(db);
            var handler = new UpdateEmployeeStatusCommandHandler(repo, new RecordingAuditLogger(), NullLogger<UpdateEmployeeStatusCommandHandler>.Instance);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                handler.Handle(new UpdateEmployeeStatusCommand { Id = Guid.NewGuid(), Status = "Active" }, CancellationToken.None));
        }

        // ─── UpdateEmployeeProfileCommandHandler ───────────────────────────────────

        [Fact]
        public async Task UpdateEmployeeProfile_UpdatesSelfServiceFieldsOnly()
        {
            using var db = CreateDb();
            var emp = NewEmployee("PRO1", "L", "One");
            emp.Designation = "Engineer";
            await db.Employees.AddAsync(emp);
            await db.SaveChangesAsync();
            var repo = new EmployeeRepository(db);
            var auditLogger = new RecordingAuditLogger();
            var handler = new UpdateEmployeeProfileCommandHandler(repo, auditLogger, NullLogger<UpdateEmployeeProfileCommandHandler>.Instance);

            await handler.Handle(new UpdateEmployeeProfileCommand
            {
                Id = emp.Id,
                PhoneNumber = "555-1234",
                Address = "1 Main St",
                EmergencyContactName = "Jane Doe",
                EmergencyContactNumber = "555-5678"
            }, CancellationToken.None);

            var updated = await repo.GetByIdAsync(emp.Id);
            Assert.Equal("555-1234", updated!.PhoneNumber);
            Assert.Equal("1 Main St", updated.Address);
            Assert.Equal("Jane Doe", updated.EmergencyContactName);
            Assert.Equal("Engineer", updated.Designation); // untouched by profile update
            Assert.Contains(auditLogger.Calls, c => c.Action == "Updated" && c.EntityId == emp.Id);
        }

        [Fact]
        public async Task UpdateEmployeeProfile_NotFound_Throws()
        {
            using var db = CreateDb();
            var repo = new EmployeeRepository(db);
            var handler = new UpdateEmployeeProfileCommandHandler(repo, new RecordingAuditLogger(), NullLogger<UpdateEmployeeProfileCommandHandler>.Instance);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                handler.Handle(new UpdateEmployeeProfileCommand { Id = Guid.NewGuid() }, CancellationToken.None));
        }

        // ─── Query handlers ─────────────────────────────────────────────────────────

        [Fact]
        public async Task GetEmployeeById_ReturnsActiveEmployee()
        {
            using var db = CreateDb();
            var emp = NewEmployee("QRY1", "M", "One");
            await db.Employees.AddAsync(emp);
            await db.SaveChangesAsync();
            var handler = new GetEmployeeByIdQueryHandler(new EmployeeRepository(db));

            var result = await handler.Handle(new GetEmployeeByIdQuery { Id = emp.Id }, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("QRY1", result!.EmployeeCode);
        }

        [Fact]
        public async Task GetEmployeeById_InactiveEmployee_ReturnsNull()
        {
            using var db = CreateDb();
            var emp = NewEmployee("QRY2", "N", "One", isActive: false);
            await db.Employees.AddAsync(emp);
            await db.SaveChangesAsync();
            var handler = new GetEmployeeByIdQueryHandler(new EmployeeRepository(db));

            var result = await handler.Handle(new GetEmployeeByIdQuery { Id = emp.Id }, CancellationToken.None);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetEmployees_FiltersBySearchTerm()
        {
            using var db = CreateDb();
            await db.Employees.AddRangeAsync(
                NewEmployee("SRCH1", "Zara", "Ahmed"),
                NewEmployee("SRCH2", "Omar", "Farouk"));
            await db.SaveChangesAsync();
            var handler = new GetEmployeesQueryHandler(new EmployeeRepository(db));

            var result = await handler.Handle(new GetEmployeesQuery { Search = "Zara" }, CancellationToken.None);

            Assert.Equal(1, result.TotalCount);
            Assert.Equal("Zara", result.Data.Single().FirstName);
        }

        [Fact]
        public async Task GetEmployees_FiltersByDepartmentAndStatus()
        {
            using var db = CreateDb();
            var deptId = Guid.NewGuid();
            await db.Employees.AddRangeAsync(
                NewEmployee("DEP1", "O", "One", departmentId: deptId, status: "Active"),
                NewEmployee("DEP2", "P", "Two", departmentId: deptId, status: "OnLeave"),
                NewEmployee("DEP3", "Q", "Three", departmentId: Guid.NewGuid(), status: "Active"));
            await db.SaveChangesAsync();
            var handler = new GetEmployeesQueryHandler(new EmployeeRepository(db));

            var result = await handler.Handle(new GetEmployeesQuery { DepartmentId = deptId, Status = "Active" }, CancellationToken.None);

            Assert.Equal(1, result.TotalCount);
            Assert.Equal("DEP1", result.Data.Single().EmployeeCode);
        }

        [Fact]
        public async Task GetEmployees_InvalidPageSize_FallsBackToDefault()
        {
            using var db = CreateDb();
            await db.Employees.AddAsync(NewEmployee("PSZ1", "R", "One"));
            await db.SaveChangesAsync();
            var handler = new GetEmployeesQueryHandler(new EmployeeRepository(db));

            var result = await handler.Handle(new GetEmployeesQuery { PageSize = 0 }, CancellationToken.None);

            Assert.Equal(20, result.PageSize);
        }

        [Fact]
        public async Task GetEmployeesByDepartment_ReturnsOnlyActiveEmployeesInDepartment()
        {
            using var db = CreateDb();
            var deptId = Guid.NewGuid();
            await db.Employees.AddRangeAsync(
                NewEmployee("BD1", "S", "One", departmentId: deptId),
                NewEmployee("BD2", "T", "Two", isActive: false, departmentId: deptId),
                NewEmployee("BD3", "U", "Three", departmentId: Guid.NewGuid()));
            await db.SaveChangesAsync();
            var handler = new GetEmployeesByDepartmentQueryHandler(new EmployeeRepository(db));

            var result = (await handler.Handle(new GetEmployeesByDepartmentQuery { DepartmentId = deptId }, CancellationToken.None)).ToList();

            Assert.Single(result);
            Assert.Equal("BD1", result[0].EmployeeCode);
        }

        [Fact]
        public async Task GetDirectReports_ReturnsOnlyActiveReportsForManager()
        {
            using var db = CreateDb();
            var manager = NewEmployee("MGR1", "V", "Boss");
            await db.Employees.AddAsync(manager);
            await db.Employees.AddRangeAsync(
                NewEmployee("REP1", "W", "One", managerId: manager.Id),
                NewEmployee("REP2", "X", "Two", isActive: false, managerId: manager.Id),
                NewEmployee("REP3", "Y", "Three"));
            await db.SaveChangesAsync();
            var handler = new GetDirectReportsQueryHandler(new EmployeeRepository(db));

            var result = (await handler.Handle(new GetDirectReportsQuery { ManagerId = manager.Id }, CancellationToken.None)).ToList();

            Assert.Single(result);
            Assert.Equal("REP1", result[0].EmployeeCode);
        }

        [Fact]
        public async Task GetReportingEmployees_ReturnsSameResultAsDirectReports()
        {
            using var db = CreateDb();
            var manager = NewEmployee("MGR2", "Z", "Boss");
            await db.Employees.AddAsync(manager);
            await db.Employees.AddAsync(NewEmployee("REP4", "AA", "One", managerId: manager.Id));
            await db.SaveChangesAsync();
            var handler = new GetReportingEmployeesQueryHandler(new EmployeeRepository(db));

            var result = (await handler.Handle(new GetReportingEmployeesQuery { ManagerId = manager.Id }, CancellationToken.None)).ToList();

            Assert.Single(result);
            Assert.Equal("REP4", result[0].EmployeeCode);
        }

        [Fact]
        public async Task GetReportingHierarchy_ReturnsManagerChainInOrder()
        {
            using var db = CreateDb();
            var vp = NewEmployee("VP1", "BB", "Exec");
            await db.Employees.AddAsync(vp);
            await db.SaveChangesAsync();
            var director = NewEmployee("DIR1", "CC", "Lead", managerId: vp.Id);
            await db.Employees.AddAsync(director);
            await db.SaveChangesAsync();
            var engineer = NewEmployee("ENG1", "DD", "Worker", managerId: director.Id);
            await db.Employees.AddAsync(engineer);
            await db.SaveChangesAsync();

            var handler = new GetReportingHierarchyQueryHandler(new EmployeeRepository(db));
            var chain = (await handler.Handle(new GetReportingHierarchyQuery { EmployeeId = engineer.Id }, CancellationToken.None)).ToList();

            Assert.Equal(2, chain.Count);
            Assert.Equal("DIR1", chain[0].EmployeeCode);
            Assert.Equal("VP1", chain[1].EmployeeCode);
        }

        // ─── EmployeeRepository ─────────────────────────────────────────────────────

        [Fact]
        public async Task EmployeeRepository_EmailExistsAsync_ExcludesSelf()
        {
            using var db = CreateDb();
            var emp = NewEmployee("SELF1", "EE", "One");
            await db.Employees.AddAsync(emp);
            await db.SaveChangesAsync();
            var repo = new EmployeeRepository(db);

            Assert.False(await repo.EmailExistsAsync(emp.Email!, emp.Id));
            Assert.True(await repo.EmailExistsAsync(emp.Email!, null));
        }

        [Fact]
        public async Task EmployeeRepository_EmployeeCodeExistsAsync_ExcludesSelf()
        {
            using var db = CreateDb();
            var emp = NewEmployee("SELF2", "FF", "One");
            await db.Employees.AddAsync(emp);
            await db.SaveChangesAsync();
            var repo = new EmployeeRepository(db);

            Assert.False(await repo.EmployeeCodeExistsAsync("SELF2", emp.Id));
            Assert.True(await repo.EmployeeCodeExistsAsync("SELF2", null));
        }

        [Fact]
        public async Task EmployeeRepository_GetByIdIncludingDeletedAsync_ReturnsInactiveEmployee()
        {
            using var db = CreateDb();
            var emp = NewEmployee("INAC1", "GG", "One", isActive: false);
            await db.Employees.AddAsync(emp);
            await db.SaveChangesAsync();
            var repo = new EmployeeRepository(db);

            Assert.Null(await repo.GetByIdAsync(emp.Id));
            Assert.NotNull(await repo.GetByIdIncludingDeletedAsync(emp.Id));
        }

        // ─── Validators ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task EmployeeCommandValidator_RejectsDuplicateEmployeeCode()
        {
            using var db = CreateDb();
            var repo = new EmployeeRepository(db);
            await repo.AddAsync(NewEmployee("VAL1", "HH", "One"));
            await repo.SaveChangesAsync();

            var validator = new EmployeeCommandValidator(repo);
            var result = await validator.ValidateAsync(new CreateEmployeeCommand { EmployeeCode = "VAL1", FirstName = "II", LastName = "Two", JoinDate = DateTime.UtcNow });

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "EmployeeCode");
        }

        [Fact]
        public async Task EmployeeCommandValidator_RejectsInvalidEmailFormat()
        {
            using var db = CreateDb();
            var repo = new EmployeeRepository(db);
            var validator = new EmployeeCommandValidator(repo);

            var result = await validator.ValidateAsync(new CreateEmployeeCommand { EmployeeCode = "VAL2", FirstName = "JJ", LastName = "One", Email = "not-an-email", JoinDate = DateTime.UtcNow });

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "Email");
        }

        [Fact]
        public async Task EmployeeCommandValidator_AcceptsValidCommand()
        {
            using var db = CreateDb();
            var repo = new EmployeeRepository(db);
            var validator = new EmployeeCommandValidator(repo);

            var result = await validator.ValidateAsync(new CreateEmployeeCommand { EmployeeCode = "VAL3", FirstName = "KK", LastName = "One", Email = "kk@test.local", JoinDate = DateTime.UtcNow });

            Assert.True(result.IsValid);
        }

        [Fact]
        public async Task UpdateEmployeeCommandValidator_AllowsKeepingOwnCodeAndEmail()
        {
            using var db = CreateDb();
            var repo = new EmployeeRepository(db);
            var emp = NewEmployee("VAL4", "LL", "One");
            await repo.AddAsync(emp);
            await repo.SaveChangesAsync();

            var validator = new UpdateEmployeeCommandValidator(repo);
            var result = await validator.ValidateAsync(new UpdateEmployeeCommand { Id = emp.Id, EmployeeCode = "VAL4", FirstName = "LL", LastName = "One", Email = emp.Email, JoinDate = emp.JoinDate });

            Assert.True(result.IsValid);
        }

        [Fact]
        public async Task UpdateEmployeeCommandValidator_RejectsCodeTakenByAnotherEmployee()
        {
            using var db = CreateDb();
            var repo = new EmployeeRepository(db);
            await repo.AddAsync(NewEmployee("VAL5", "MM", "One"));
            var second = NewEmployee("VAL6", "NN", "Two");
            await repo.AddAsync(second);
            await repo.SaveChangesAsync();

            var validator = new UpdateEmployeeCommandValidator(repo);
            var result = await validator.ValidateAsync(new UpdateEmployeeCommand { Id = second.Id, EmployeeCode = "VAL5", FirstName = "NN", LastName = "Two", JoinDate = second.JoinDate });

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "EmployeeCode");
        }
    }
}
