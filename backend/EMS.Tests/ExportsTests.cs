using ClosedXML.Excel;
using EMS.API.Controllers;
using EMS.Application.Common.DTOs;
using EMS.Application.DTOs;
using EMS.Application.Features.Attendance.DTOs;
using EMS.Application.Features.Exports.Handlers;
using EMS.Application.Features.Exports.Queries;
using EMS.Application.Features.Exports.Validators;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using EMS.Infrastructure.Export;
using EMS.Infrastructure.Pdf;
using EMS.Persistence.Context;
using EMS.Persistence.Repositories;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EMS.Tests
{
    public class ExportsTests
    {
        private static ApplicationDbContext CreateDb(string name) =>
            new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(name).Options);

        // ─── Repository tests ───────────────────────────────────────────────────────

        [Fact]
        public async Task EmployeeRepository_GetAllForExportAsync_ReturnsAllMatchingEmployees_WithoutPagination()
        {
            using var db = CreateDb("ems_export_emp_" + Guid.NewGuid());
            var companyId = Guid.NewGuid();
            var dept = new Department { Id = Guid.NewGuid(), CompanyId = companyId, Name = "Engineering" };
            var designation = new Designation { Id = Guid.NewGuid(), CompanyId = companyId, Name = "Engineer", Code = "ENG", CreatedAtUtc = DateTime.UtcNow };
            var officeLocation = new OfficeLocation { Id = Guid.NewGuid(), CompanyId = companyId, Name = "HQ", Code = "HQ", City = "City", Country = "Country", TimeZoneId = "UTC", CreatedAtUtc = DateTime.UtcNow };
            db.Departments.Add(dept);
            db.Designations.Add(designation);
            db.OfficeLocations.Add(officeLocation);
            for (var i = 0; i < 25; i++)
            {
                db.Employees.Add(new Employee
                {
                    Id = Guid.NewGuid(),
                    CompanyId = companyId,
                    EmployeeCode = $"E{i:000}",
                    FirstName = "First" + i,
                    LastName = "Last" + i,
                    DepartmentId = dept.Id,
                    DesignationId = designation.Id,
                    OfficeLocationId = officeLocation.Id,
                    EmploymentStatus = "Active",
                    IsActive = true,
                    JoinDate = DateTime.UtcNow
                });
            }
            await db.SaveChangesAsync();

            var repo = new EmployeeRepository(db);
            var result = (await repo.GetAllForExportAsync(companyId, null, null, null, dept.Id, "Active", ct: CancellationToken.None)).ToList();

            Assert.Equal(25, result.Count);
            Assert.All(result, e => Assert.Equal("Engineering", e.Department!.Name));
        }

        [Fact]
        public async Task EmployeeRepository_GetByIdsAsync_ReturnsOnlyRequestedEmployees()
        {
            using var db = CreateDb("ems_export_empids_" + Guid.NewGuid());
            var e1 = new Employee { Id = Guid.NewGuid(), EmployeeCode = "E1", FirstName = "A", LastName = "One", IsActive = true, JoinDate = DateTime.UtcNow };
            var e2 = new Employee { Id = Guid.NewGuid(), EmployeeCode = "E2", FirstName = "B", LastName = "Two", IsActive = true, JoinDate = DateTime.UtcNow };
            var e3 = new Employee { Id = Guid.NewGuid(), EmployeeCode = "E3", FirstName = "C", LastName = "Three", IsActive = true, JoinDate = DateTime.UtcNow };
            db.Employees.AddRange(e1, e2, e3);
            await db.SaveChangesAsync();

            var repo = new EmployeeRepository(db);
            var result = (await repo.GetByIdsAsync(new[] { e1.Id, e3.Id }, CancellationToken.None)).ToList();

            Assert.Equal(2, result.Count);
            Assert.Contains(result, e => e.Id == e1.Id);
            Assert.Contains(result, e => e.Id == e3.Id);
            Assert.DoesNotContain(result, e => e.Id == e2.Id);
        }

        [Fact]
        public async Task AttendanceRepository_GetAllRecordsAsync_FiltersByDateRangeAndStatus_WithoutPagination()
        {
            using var db = CreateDb("ems_export_att_" + Guid.NewGuid());
            var employeeId = Guid.NewGuid();
            for (var i = 0; i < 10; i++)
            {
                db.AttendanceRecords.Add(new AttendanceRecord
                {
                    Id = Guid.NewGuid(),
                    EmployeeId = employeeId,
                    AttendanceDate = new DateTime(2026, 1, 1).AddDays(i),
                    Status = i % 2 == 0 ? AttendanceStatus.Present : AttendanceStatus.Absent
                });
            }
            await db.SaveChangesAsync();

            var repo = new AttendanceRepository(db);
            var filter = new AttendanceRecordFilter
            {
                DateFrom = new DateTime(2026, 1, 3),
                DateTo = new DateTime(2026, 1, 7),
                Status = "Present"
            };

            var result = (await repo.GetAllRecordsAsync(filter, CancellationToken.None)).ToList();

            Assert.All(result, r => Assert.Equal(AttendanceStatus.Present, r.Status));
            Assert.All(result, r => Assert.InRange(r.AttendanceDate, filter.DateFrom.Value, filter.DateTo.Value));
        }

        [Fact]
        public async Task LeaveRepository_GetAllLeavesAsync_FiltersByEmployeeAndStatus_WithoutPagination()
        {
            using var db = CreateDb("ems_export_leave_" + Guid.NewGuid());
            var employeeId = Guid.NewGuid();
            var otherEmployeeId = Guid.NewGuid();
            var leaveTypeId = Guid.NewGuid();

            db.LeaveRequests.AddRange(
                new LeaveRequest { Id = Guid.NewGuid(), EmployeeId = employeeId, LeaveTypeId = leaveTypeId, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow, Status = LeaveStatus.Approved },
                new LeaveRequest { Id = Guid.NewGuid(), EmployeeId = employeeId, LeaveTypeId = leaveTypeId, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow, Status = LeaveStatus.Pending },
                new LeaveRequest { Id = Guid.NewGuid(), EmployeeId = otherEmployeeId, LeaveTypeId = leaveTypeId, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow, Status = LeaveStatus.Approved });
            await db.SaveChangesAsync();

            var repo = new LeaveRepository(db);
            var result = (await repo.GetAllLeavesAsync(employeeId, null, null, "Approved", CancellationToken.None)).ToList();

            Assert.Single(result);
            Assert.Equal(employeeId, result[0].EmployeeId);
        }

        [Fact]
        public async Task ReimbursementRepository_GetAllForExportAsync_FiltersByEmployeeAndStatus_WithoutPagination()
        {
            using var db = CreateDb("ems_export_reimb_" + Guid.NewGuid());
            var employee = new Employee { Id = Guid.NewGuid(), EmployeeCode = "E1", FirstName = "Ada", LastName = "Lovelace", IsActive = true, JoinDate = DateTime.UtcNow };
            var otherEmployee = new Employee { Id = Guid.NewGuid(), EmployeeCode = "E2", FirstName = "Bob", LastName = "Smith", IsActive = true, JoinDate = DateTime.UtcNow };
            db.Employees.AddRange(employee, otherEmployee);
            db.Reimbursements.AddRange(
                new Reimbursement { Id = Guid.NewGuid(), ReimbursementNumber = "REI-1", EmployeeId = employee.Id, ExpenseTitle = "Taxi", ExpenseCategory = "Travel", ExpenseDate = DateTime.UtcNow, Amount = 50, Status = ReimbursementStatus.Approved, CreatedAtUtc = DateTime.UtcNow },
                new Reimbursement { Id = Guid.NewGuid(), ReimbursementNumber = "REI-2", EmployeeId = employee.Id, ExpenseTitle = "Hotel", ExpenseCategory = "Travel", ExpenseDate = DateTime.UtcNow, Amount = 200, Status = ReimbursementStatus.Draft, CreatedAtUtc = DateTime.UtcNow },
                new Reimbursement { Id = Guid.NewGuid(), ReimbursementNumber = "REI-3", EmployeeId = otherEmployee.Id, ExpenseTitle = "Meal", ExpenseCategory = "Food", ExpenseDate = DateTime.UtcNow, Amount = 20, Status = ReimbursementStatus.Approved, CreatedAtUtc = DateTime.UtcNow });
            await db.SaveChangesAsync();

            var repo = new ReimbursementRepository(db);
            var result = (await repo.GetAllForExportAsync(employee.Id, ReimbursementStatus.Approved, CancellationToken.None)).ToList();

            var reimbursement = Assert.Single(result);
            Assert.Equal("REI-1", reimbursement.ReimbursementNumber);
        }

        [Fact]
        public async Task AssetRepository_GetAllForExportAsync_FiltersByStatusCategoryAndSearch_WithoutPagination()
        {
            using var db = CreateDb("ems_export_asset_" + Guid.NewGuid());
            db.Assets.AddRange(
                new Asset { Id = Guid.NewGuid(), AssetTag = "LAP-001", Category = "Laptop", Status = AssetStatus.Assigned, CreatedAtUtc = DateTime.UtcNow },
                new Asset { Id = Guid.NewGuid(), AssetTag = "LAP-002", Category = "Laptop", Status = AssetStatus.Available, CreatedAtUtc = DateTime.UtcNow },
                new Asset { Id = Guid.NewGuid(), AssetTag = "MOB-001", Category = "Mobile", Status = AssetStatus.Assigned, CreatedAtUtc = DateTime.UtcNow });
            await db.SaveChangesAsync();

            var repo = new AssetRepository(db);
            var result = (await repo.GetAllForExportAsync(AssetStatus.Assigned, "Laptop", null, CancellationToken.None)).ToList();

            var asset = Assert.Single(result);
            Assert.Equal("LAP-001", asset.AssetTag);
        }

        [Fact]
        public async Task AssetRepository_GetActiveAssignmentsByAssetIdsAsync_ReturnsOnlyActiveAssignmentsWithEmployee()
        {
            using var db = CreateDb("ems_export_assetassign_" + Guid.NewGuid());
            var employee = new Employee { Id = Guid.NewGuid(), EmployeeCode = "E1", FirstName = "Ada", LastName = "Lovelace", IsActive = true, JoinDate = DateTime.UtcNow };
            var assetA = new Asset { Id = Guid.NewGuid(), AssetTag = "LAP-001", Category = "Laptop", Status = AssetStatus.Assigned, CreatedAtUtc = DateTime.UtcNow };
            var assetB = new Asset { Id = Guid.NewGuid(), AssetTag = "LAP-002", Category = "Laptop", Status = AssetStatus.Available, CreatedAtUtc = DateTime.UtcNow };
            db.Employees.Add(employee);
            db.Assets.AddRange(assetA, assetB);
            db.AssetAssignments.AddRange(
                new AssetAssignment { Id = Guid.NewGuid(), AssetId = assetA.Id, EmployeeId = employee.Id, AssignedByUserId = Guid.NewGuid(), AssignedDate = DateTime.UtcNow, CreatedAtUtc = DateTime.UtcNow },
                new AssetAssignment { Id = Guid.NewGuid(), AssetId = assetB.Id, EmployeeId = employee.Id, AssignedByUserId = Guid.NewGuid(), AssignedDate = DateTime.UtcNow, ReturnedDate = DateTime.UtcNow, CreatedAtUtc = DateTime.UtcNow });
            await db.SaveChangesAsync();

            var repo = new AssetRepository(db);
            var result = (await repo.GetActiveAssignmentsByAssetIdsAsync(new[] { assetA.Id, assetB.Id }, CancellationToken.None)).ToList();

            var assignment = Assert.Single(result);
            Assert.Equal(assetA.Id, assignment.AssetId);
            Assert.NotNull(assignment.Employee);
        }

        [Fact]
        public async Task RecruitmentRepository_GetAllForExportAsync_FiltersByStatusDesignationAndSearch_WithoutPagination()
        {
            using var db = CreateDb("ems_export_candidate_" + Guid.NewGuid());
            var designation = new Designation { Id = Guid.NewGuid(), Name = "Engineer", Code = "ENG", CreatedAtUtc = DateTime.UtcNow };
            var otherDesignation = new Designation { Id = Guid.NewGuid(), Name = "Sales", Code = "SLS", CreatedAtUtc = DateTime.UtcNow };
            db.Designations.AddRange(designation, otherDesignation);
            db.Candidates.AddRange(
                new Candidate { Id = Guid.NewGuid(), CandidateNumber = "CAN-1", FirstName = "Ada", LastName = "Lovelace", Email = "ada@test.com", DesignationId = designation.Id, AppliedDate = DateTime.UtcNow, Status = CandidateStatus.Screening, CreatedAtUtc = DateTime.UtcNow },
                new Candidate { Id = Guid.NewGuid(), CandidateNumber = "CAN-2", FirstName = "Bob", LastName = "Smith", Email = "bob@test.com", DesignationId = designation.Id, AppliedDate = DateTime.UtcNow, Status = CandidateStatus.Applied, CreatedAtUtc = DateTime.UtcNow },
                new Candidate { Id = Guid.NewGuid(), CandidateNumber = "CAN-3", FirstName = "Cara", LastName = "Jones", Email = "cara@test.com", DesignationId = otherDesignation.Id, AppliedDate = DateTime.UtcNow, Status = CandidateStatus.Screening, CreatedAtUtc = DateTime.UtcNow });
            await db.SaveChangesAsync();

            var repo = new RecruitmentRepository(db);
            var result = (await repo.GetAllForExportAsync(CandidateStatus.Screening, designation.Id, null, CancellationToken.None)).ToList();

            var candidate = Assert.Single(result);
            Assert.Equal("CAN-1", candidate.CandidateNumber);
        }

        // ─── Handler tests ──────────────────────────────────────────────────────────

        [Fact]
        public async Task ExportEmployeesQueryHandler_Handle_GeneratesExcelWithCorrectFileNameAndContentType()
        {
            var employeeRepo = new Mock<IEmployeeRepository>();
            employeeRepo
                .Setup(r => r.GetAllForExportAsync(It.IsAny<Guid>(), null, null, null, null, null, null, null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[]
                {
                    new Employee { Id = Guid.NewGuid(), EmployeeCode = "E1", FirstName = "Ada", LastName = "Lovelace", IsActive = true, JoinDate = DateTime.UtcNow }
                });

            var excelService = new Mock<IExcelExportService>();
            excelService
                .Setup(s => s.GenerateAsync("Employees", It.IsAny<IReadOnlyList<string>>(), It.IsAny<IEnumerable<IReadOnlyList<object?>>>()))
                .ReturnsAsync(new byte[] { 1, 2, 3 });

            var handler = new ExportEmployeesQueryHandler(employeeRepo.Object, excelService.Object, Mock.Of<ICurrentUserService>(c => c.CompanyId == Guid.NewGuid()));

            var result = await handler.Handle(new ExportEmployeesQuery(), CancellationToken.None);

            Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", result.ContentType);
            Assert.StartsWith("employees_", result.FileName);
            Assert.EndsWith(".xlsx", result.FileName);
            Assert.Equal(new byte[] { 1, 2, 3 }, result.Content);
            excelService.Verify(s => s.GenerateAsync("Employees", It.IsAny<IReadOnlyList<string>>(),
                It.Is<IEnumerable<IReadOnlyList<object?>>>(rows => rows.Count() == 1)), Times.Once);
        }

        [Fact]
        public async Task ExportAttendanceQueryHandler_Handle_AdminOrHr_DoesNotRestrictEmployeeScope()
        {
            var attendanceRepo = new Mock<IAttendanceRepository>();
            attendanceRepo
                .Setup(r => r.GetAllRecordsAsync(It.Is<AttendanceRecordFilter>(f => f.EmployeeIdsScope == null), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Enumerable.Empty<AttendanceRecord>());

            var employeeRepo = new Mock<IEmployeeRepository>();
            employeeRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Enumerable.Empty<Employee>());

            var excelService = new Mock<IExcelExportService>();
            excelService.Setup(s => s.GenerateAsync("Attendance", It.IsAny<IReadOnlyList<string>>(), It.IsAny<IEnumerable<IReadOnlyList<object?>>>()))
                .ReturnsAsync(Array.Empty<byte>());

            var handler = new ExportAttendanceQueryHandler(attendanceRepo.Object, Mock.Of<IAuthRepository>(), employeeRepo.Object, excelService.Object);

            var result = await handler.Handle(new ExportAttendanceQuery { IsAdminOrHr = true }, CancellationToken.None);

            Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", result.ContentType);
            attendanceRepo.Verify(r => r.GetAllRecordsAsync(It.Is<AttendanceRecordFilter>(f => f.EmployeeIdsScope == null), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExportAttendanceQueryHandler_Handle_Manager_RestrictsToOwnTeam()
        {
            var managerUserId = Guid.NewGuid();
            var managerEmployeeId = Guid.NewGuid();
            var reportIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

            var authRepo = new Mock<IAuthRepository>();
            authRepo.Setup(r => r.GetByIdAsync(managerUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new User { Id = managerUserId, EmployeeId = managerEmployeeId, UserName = "mgr", Email = "mgr@test.com", PasswordHash = "x" });

            var employeeRepo = new Mock<IEmployeeRepository>();
            employeeRepo.Setup(r => r.GetDirectReportIdsAsync(managerEmployeeId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(reportIds);
            employeeRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Enumerable.Empty<Employee>());

            AttendanceRecordFilter? capturedFilter = null;
            var attendanceRepo = new Mock<IAttendanceRepository>();
            attendanceRepo
                .Setup(r => r.GetAllRecordsAsync(It.IsAny<AttendanceRecordFilter>(), It.IsAny<CancellationToken>()))
                .Callback<AttendanceRecordFilter, CancellationToken>((f, _) => capturedFilter = f)
                .ReturnsAsync(Enumerable.Empty<AttendanceRecord>());

            var excelService = new Mock<IExcelExportService>();
            excelService.Setup(s => s.GenerateAsync("Attendance", It.IsAny<IReadOnlyList<string>>(), It.IsAny<IEnumerable<IReadOnlyList<object?>>>()))
                .ReturnsAsync(Array.Empty<byte>());

            var handler = new ExportAttendanceQueryHandler(attendanceRepo.Object, authRepo.Object, employeeRepo.Object, excelService.Object);

            await handler.Handle(new ExportAttendanceQuery { IsAdminOrHr = false, IsManager = true, RequestingUserId = managerUserId }, CancellationToken.None);

            Assert.NotNull(capturedFilter);
            Assert.NotNull(capturedFilter!.EmployeeIdsScope);
            Assert.Contains(managerEmployeeId, capturedFilter.EmployeeIdsScope!);
            Assert.All(reportIds, id => Assert.Contains(id, capturedFilter.EmployeeIdsScope!));
        }

        [Fact]
        public async Task ExportAttendanceQueryHandler_Handle_Manager_RequestingOutsideTeamEmployee_Throws()
        {
            var managerUserId = Guid.NewGuid();
            var managerEmployeeId = Guid.NewGuid();
            var outsiderEmployeeId = Guid.NewGuid();

            var authRepo = new Mock<IAuthRepository>();
            authRepo.Setup(r => r.GetByIdAsync(managerUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new User { Id = managerUserId, EmployeeId = managerEmployeeId, UserName = "mgr", Email = "mgr@test.com", PasswordHash = "x" });

            var employeeRepo = new Mock<IEmployeeRepository>();
            employeeRepo.Setup(r => r.GetDirectReportIdsAsync(managerEmployeeId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Guid>());

            var handler = new ExportAttendanceQueryHandler(
                Mock.Of<IAttendanceRepository>(), authRepo.Object, employeeRepo.Object, Mock.Of<IExcelExportService>());

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(
                new ExportAttendanceQuery { IsManager = true, RequestingUserId = managerUserId, EmployeeId = outsiderEmployeeId },
                CancellationToken.None));
        }

        [Fact]
        public async Task ExportLeaveRequestsQueryHandler_Handle_GeneratesExcelWithResolvedLeaveTypeAndEmployeeNames()
        {
            var employee = new Employee { Id = Guid.NewGuid(), EmployeeCode = "E1", FirstName = "Ada", LastName = "Lovelace", IsActive = true, JoinDate = DateTime.UtcNow };
            var leaveType = new LeaveType { Id = Guid.NewGuid(), Name = "Annual Leave" };
            var leaveRequest = new LeaveRequest
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                LeaveTypeId = leaveType.Id,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow,
                Status = LeaveStatus.Approved
            };

            var leaveRepo = new Mock<ILeaveRepository>();
            leaveRepo.Setup(r => r.GetAllLeavesAsync(null, null, null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { leaveRequest });
            leaveRepo.Setup(r => r.GetLeaveTypeByIdIncludingDeletedAsync(leaveType.Id, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(leaveType);

            var employeeRepo = new Mock<IEmployeeRepository>();
            employeeRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { employee });

            IEnumerable<IReadOnlyList<object?>>? capturedRows = null;
            var excelService = new Mock<IExcelExportService>();
            excelService
                .Setup(s => s.GenerateAsync("Leave Requests", It.IsAny<IReadOnlyList<string>>(), It.IsAny<IEnumerable<IReadOnlyList<object?>>>()))
                .Callback<string, IReadOnlyList<string>, IEnumerable<IReadOnlyList<object?>>>((_, _, rows) => capturedRows = rows)
                .ReturnsAsync(Array.Empty<byte>());

            var handler = new ExportLeaveRequestsQueryHandler(leaveRepo.Object, employeeRepo.Object, excelService.Object, Mock.Of<ICurrentUserService>(c => c.CompanyId == Guid.NewGuid()));

            var result = await handler.Handle(new ExportLeaveRequestsQuery(), CancellationToken.None);

            Assert.StartsWith("leave-requests_", result.FileName);
            var row = Assert.Single(capturedRows!);
            Assert.Equal("E1", row[0]);
            Assert.Equal("Ada Lovelace", row[1]);
            Assert.Equal("Annual Leave", row[2]);
        }

        [Fact]
        public async Task ExportDashboardSummaryQueryHandler_Handle_GeneratesPdfFromSummary()
        {
            var summary = new DashboardSummaryDto { TotalEmployees = 10, ActiveEmployees = 8, InactiveEmployees = 2 };
            var repo = new Mock<IDashboardRepository>();
            repo.Setup(r => r.GetSummaryAsync(It.IsAny<Guid>(), null, It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(summary);

            var pdfService = new Mock<IPdfService>();
            pdfService.Setup(p => p.GenerateDashboardSummaryPdfAsync(summary, It.IsAny<DateTime>(), null))
                .ReturnsAsync(new byte[] { 9, 9, 9 });

            var handler = new ExportDashboardSummaryQueryHandler(repo.Object, Mock.Of<ICurrentUserService>(c => c.CompanyId == Guid.NewGuid()), pdfService.Object);

            var result = await handler.Handle(new ExportDashboardSummaryQuery(), CancellationToken.None);

            Assert.Equal("application/pdf", result.ContentType);
            Assert.StartsWith("dashboard-summary_", result.FileName);
            Assert.EndsWith(".pdf", result.FileName);
            Assert.Equal(new byte[] { 9, 9, 9 }, result.Content);
        }

        [Fact]
        public async Task ExportReimbursementsQueryHandler_Handle_NonPrivileged_ScopesToOwnEmployeeRegardlessOfFilter()
        {
            var requestingUserId = Guid.NewGuid();
            var ownEmployeeId = Guid.NewGuid();
            var otherEmployeeId = Guid.NewGuid();

            var authRepo = new Mock<IAuthRepository>();
            authRepo.Setup(r => r.GetByIdAsync(requestingUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new User { Id = requestingUserId, EmployeeId = ownEmployeeId, UserName = "u", Email = "u@test.com", PasswordHash = "x" });

            Guid? capturedEmployeeId = null;
            var reimbursementRepo = new Mock<IReimbursementRepository>();
            reimbursementRepo
                .Setup(r => r.GetAllForExportAsync(It.IsAny<Guid?>(), null, It.IsAny<CancellationToken>()))
                .Callback<Guid?, ReimbursementStatus?, CancellationToken>((employeeId, _, _) => capturedEmployeeId = employeeId)
                .ReturnsAsync(Enumerable.Empty<Reimbursement>());

            var excelService = new Mock<IExcelExportService>();
            excelService.Setup(s => s.GenerateAsync("Reimbursements", It.IsAny<IReadOnlyList<string>>(), It.IsAny<IEnumerable<IReadOnlyList<object?>>>()))
                .ReturnsAsync(Array.Empty<byte>());

            var handler = new ExportReimbursementsQueryHandler(reimbursementRepo.Object, authRepo.Object, excelService.Object);

            await handler.Handle(new ExportReimbursementsQuery { IsPrivileged = false, RequestingUserId = requestingUserId, EmployeeId = otherEmployeeId }, CancellationToken.None);

            Assert.Equal(ownEmployeeId, capturedEmployeeId);
        }

        [Fact]
        public async Task ExportReimbursementsQueryHandler_Handle_Privileged_UsesRequestedEmployeeFilter()
        {
            var otherEmployeeId = Guid.NewGuid();
            Guid? capturedEmployeeId = null;

            var reimbursementRepo = new Mock<IReimbursementRepository>();
            reimbursementRepo
                .Setup(r => r.GetAllForExportAsync(It.IsAny<Guid?>(), null, It.IsAny<CancellationToken>()))
                .Callback<Guid?, ReimbursementStatus?, CancellationToken>((employeeId, _, _) => capturedEmployeeId = employeeId)
                .ReturnsAsync(Enumerable.Empty<Reimbursement>());

            var excelService = new Mock<IExcelExportService>();
            excelService.Setup(s => s.GenerateAsync("Reimbursements", It.IsAny<IReadOnlyList<string>>(), It.IsAny<IEnumerable<IReadOnlyList<object?>>>()))
                .ReturnsAsync(Array.Empty<byte>());

            var handler = new ExportReimbursementsQueryHandler(reimbursementRepo.Object, Mock.Of<IAuthRepository>(), excelService.Object);

            var result = await handler.Handle(new ExportReimbursementsQuery { IsPrivileged = true, EmployeeId = otherEmployeeId }, CancellationToken.None);

            Assert.Equal(otherEmployeeId, capturedEmployeeId);
            Assert.StartsWith("reimbursements_", result.FileName);
        }

        [Fact]
        public async Task ExportAssetsQueryHandler_Handle_IncludesCurrentAssigneeName()
        {
            var asset = new Asset { Id = Guid.NewGuid(), AssetTag = "LAP-001", Category = "Laptop", Status = AssetStatus.Assigned };
            var employee = new Employee { Id = Guid.NewGuid(), EmployeeCode = "E1", FirstName = "Ada", LastName = "Lovelace", IsActive = true, JoinDate = DateTime.UtcNow };
            var assignment = new AssetAssignment { Id = Guid.NewGuid(), AssetId = asset.Id, EmployeeId = employee.Id, Employee = employee, AssignedByUserId = Guid.NewGuid(), AssignedDate = DateTime.UtcNow };

            var assetRepo = new Mock<IAssetRepository>();
            assetRepo.Setup(r => r.GetAllForExportAsync(null, null, null, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { asset });
            assetRepo.Setup(r => r.GetActiveAssignmentsByAssetIdsAsync(It.Is<IEnumerable<Guid>>(ids => ids.Contains(asset.Id)), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { assignment });

            IEnumerable<IReadOnlyList<object?>>? capturedRows = null;
            var excelService = new Mock<IExcelExportService>();
            excelService.Setup(s => s.GenerateAsync("Assets", It.IsAny<IReadOnlyList<string>>(), It.IsAny<IEnumerable<IReadOnlyList<object?>>>()))
                .Callback<string, IReadOnlyList<string>, IEnumerable<IReadOnlyList<object?>>>((_, _, rows) => capturedRows = rows)
                .ReturnsAsync(Array.Empty<byte>());

            var handler = new ExportAssetsQueryHandler(assetRepo.Object, excelService.Object);

            var result = await handler.Handle(new ExportAssetsQuery(), CancellationToken.None);

            Assert.StartsWith("assets_", result.FileName);
            var row = Assert.Single(capturedRows!);
            Assert.Equal("LAP-001", row[0]);
            Assert.Equal("Ada Lovelace", row[8]);
        }

        [Fact]
        public async Task ExportCandidatesQueryHandler_Handle_GeneratesExcelWithResolvedDesignationAndDepartmentNames()
        {
            var designation = new Designation { Id = Guid.NewGuid(), Name = "Engineer", Code = "ENG" };
            var department = new Department { Id = Guid.NewGuid(), Name = "Engineering" };
            var candidate = new Candidate
            {
                Id = Guid.NewGuid(), CandidateNumber = "CAN-1", FirstName = "Ada", LastName = "Lovelace", Email = "ada@test.com",
                DesignationId = designation.Id, Designation = designation, DepartmentId = department.Id, Department = department,
                AppliedDate = DateTime.UtcNow, Status = CandidateStatus.Screening
            };

            var recruitmentRepo = new Mock<IRecruitmentRepository>();
            recruitmentRepo.Setup(r => r.GetAllForExportAsync(null, null, null, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { candidate });

            IEnumerable<IReadOnlyList<object?>>? capturedRows = null;
            var excelService = new Mock<IExcelExportService>();
            excelService.Setup(s => s.GenerateAsync("Candidates", It.IsAny<IReadOnlyList<string>>(), It.IsAny<IEnumerable<IReadOnlyList<object?>>>()))
                .Callback<string, IReadOnlyList<string>, IEnumerable<IReadOnlyList<object?>>>((_, _, rows) => capturedRows = rows)
                .ReturnsAsync(Array.Empty<byte>());

            var handler = new ExportCandidatesQueryHandler(recruitmentRepo.Object, excelService.Object);

            var result = await handler.Handle(new ExportCandidatesQuery(), CancellationToken.None);

            Assert.StartsWith("candidates_", result.FileName);
            var row = Assert.Single(capturedRows!);
            Assert.Equal("CAN-1", row[0]);
            Assert.Equal("Engineer", row[5]);
            Assert.Equal("Engineering", row[6]);
        }

        // ─── Service tests (real implementations) ──────────────────────────────────

        [Fact]
        public async Task ClosedXmlExportService_GenerateAsync_ProducesReadableWorkbookWithHeadersAndRows()
        {
            var service = new ClosedXmlExportService();
            var headers = new[] { "Name", "Age" };
            var rows = new List<IReadOnlyList<object?>>
            {
                new List<object?> { "Ada", 30 },
                new List<object?> { null, null }
            };

            var bytes = await service.GenerateAsync("People", headers, rows);

            Assert.NotEmpty(bytes);
            using var stream = new MemoryStream(bytes);
            using var workbook = new XLWorkbook(stream);
            var sheet = workbook.Worksheet("People");

            Assert.Equal("Name", sheet.Cell(1, 1).GetString());
            Assert.Equal("Age", sheet.Cell(1, 2).GetString());
            Assert.Equal("Ada", sheet.Cell(2, 1).GetString());
            Assert.Equal(30, sheet.Cell(2, 2).GetDouble());
            Assert.Equal(string.Empty, sheet.Cell(3, 1).GetString());
        }

        [Fact]
        public async Task PdfSharpDocumentService_GenerateDashboardSummaryPdfAsync_ReturnsNonEmptyPdfBytes()
        {
            var service = new PdfSharpDocumentService();
            var summary = new DashboardSummaryDto
            {
                TotalEmployees = 5,
                ActiveEmployees = 4,
                InactiveEmployees = 1,
                Departments = new[] { new DepartmentSummaryDto { DepartmentId = Guid.NewGuid(), DepartmentName = "Engineering", ActiveEmployees = 4 } }
            };

            var bytes = await service.GenerateDashboardSummaryPdfAsync(summary, DateTime.UtcNow.Date, null);

            Assert.NotEmpty(bytes);
            Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
        }

        // ─── Controller tests ───────────────────────────────────────────────────────

        [Fact]
        public async Task ExportsController_ExportEmployees_ReturnsFileResult()
        {
            var mediatorMock = new Mock<IMediator>();
            mediatorMock.Setup(m => m.Send(It.IsAny<ExportEmployeesQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExportFileResult { Content = new byte[] { 1 }, ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", FileName = "employees_x.xlsx" });

            var controller = new ExportsController(mediatorMock.Object);

            var result = await controller.ExportEmployees(new ExportEmployeesQuery(), CancellationToken.None);

            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("employees_x.xlsx", fileResult.FileDownloadName);
            Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileResult.ContentType);
        }

        [Fact]
        public async Task ExportsController_ExportAttendance_SetsRequestingUserIdAndRoleFlagsFromClaims()
        {
            var userId = Guid.NewGuid();
            ExportAttendanceQuery? sentQuery = null;

            var mediatorMock = new Mock<IMediator>();
            mediatorMock.Setup(m => m.Send(It.IsAny<ExportAttendanceQuery>(), It.IsAny<CancellationToken>()))
                .Callback<object, CancellationToken>((q, _) => sentQuery = (ExportAttendanceQuery)q)
                .ReturnsAsync(new ExportFileResult { Content = Array.Empty<byte>(), ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", FileName = "attendance_x.xlsx" });

            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId.ToString()), new(ClaimTypes.Role, "Manager") };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

            var controller = new ExportsController(mediatorMock.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = principal } }
            };

            await controller.ExportAttendance(new ExportAttendanceQuery(), CancellationToken.None);

            Assert.NotNull(sentQuery);
            Assert.Equal(userId, sentQuery!.RequestingUserId);
            Assert.True(sentQuery.IsManager);
            Assert.False(sentQuery.IsAdminOrHr);
        }

        [Fact]
        public async Task ExportsController_ExportReimbursements_SetsRequestingUserIdAndPrivilegeFromClaims()
        {
            var userId = Guid.NewGuid();
            ExportReimbursementsQuery? sentQuery = null;

            var mediatorMock = new Mock<IMediator>();
            mediatorMock.Setup(m => m.Send(It.IsAny<ExportReimbursementsQuery>(), It.IsAny<CancellationToken>()))
                .Callback<object, CancellationToken>((q, _) => sentQuery = (ExportReimbursementsQuery)q)
                .ReturnsAsync(new ExportFileResult { Content = Array.Empty<byte>(), ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", FileName = "reimbursements_x.xlsx" });

            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId.ToString()) };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

            var controller = new ExportsController(mediatorMock.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = principal } }
            };

            await controller.ExportReimbursements(new ExportReimbursementsQuery(), CancellationToken.None);

            Assert.NotNull(sentQuery);
            Assert.Equal(userId, sentQuery!.RequestingUserId);
            Assert.False(sentQuery.IsPrivileged);
        }

        [Fact]
        public async Task ExportsController_ExportAssets_ReturnsFileResult()
        {
            var mediatorMock = new Mock<IMediator>();
            mediatorMock.Setup(m => m.Send(It.IsAny<ExportAssetsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExportFileResult { Content = new byte[] { 1 }, ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", FileName = "assets_x.xlsx" });

            var controller = new ExportsController(mediatorMock.Object);

            var result = await controller.ExportAssets(new ExportAssetsQuery(), CancellationToken.None);

            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("assets_x.xlsx", fileResult.FileDownloadName);
        }

        [Fact]
        public async Task ExportsController_ExportCandidates_ReturnsFileResult()
        {
            var mediatorMock = new Mock<IMediator>();
            mediatorMock.Setup(m => m.Send(It.IsAny<ExportCandidatesQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExportFileResult { Content = new byte[] { 1 }, ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", FileName = "candidates_x.xlsx" });

            var controller = new ExportsController(mediatorMock.Object);

            var result = await controller.ExportCandidates(new ExportCandidatesQuery(), CancellationToken.None);

            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("candidates_x.xlsx", fileResult.FileDownloadName);
        }

        // ─── Validator tests ────────────────────────────────────────────────────────

        [Fact]
        public void ExportAttendanceQueryValidator_Invalid_WhenDateFromAfterDateTo()
        {
            var validator = new ExportAttendanceQueryValidator();
            var result = validator.Validate(new ExportAttendanceQuery { DateFrom = new DateTime(2026, 2, 1), DateTo = new DateTime(2026, 1, 1) });

            Assert.False(result.IsValid);
        }

        [Fact]
        public void ExportAttendanceQueryValidator_Valid_WhenDatesOmittedOrInOrder()
        {
            var validator = new ExportAttendanceQueryValidator();

            Assert.True(validator.Validate(new ExportAttendanceQuery()).IsValid);
            Assert.True(validator.Validate(new ExportAttendanceQuery { DateFrom = new DateTime(2026, 1, 1), DateTo = new DateTime(2026, 1, 31) }).IsValid);
        }

        [Fact]
        public void ExportDashboardSummaryQueryValidator_Invalid_WhenDateInFuture()
        {
            var validator = new ExportDashboardSummaryQueryValidator();
            var result = validator.Validate(new ExportDashboardSummaryQuery { Date = DateTime.UtcNow.AddDays(1) });

            Assert.False(result.IsValid);
        }
    }
}
