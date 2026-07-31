using EMS.Application.Features.Maintenance.Commands;
using EMS.Application.Features.Maintenance.Handlers;
using EMS.Application.Features.Performance.Commands;
using EMS.Application.Features.Performance.Handlers;
using EMS.Application.Features.Recruitment.Commands;
using EMS.Application.Features.Recruitment.DTOs;
using EMS.Application.Features.Recruitment.Handlers;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
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
    public class MaintenanceSweepTests
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

        private class FakePdfService : IPdfService
        {
            public Task<byte[]> GeneratePayslipPdfAsync(EMS.Application.Interfaces.PayslipDocument document) => Task.FromResult(new byte[] { 1 });
            public Task<byte[]> GenerateDashboardSummaryPdfAsync(EMS.Application.DTOs.DashboardSummaryDto summary, DateTime asOfDate, Guid? departmentId) => Task.FromResult(new byte[] { 1 });
            public Task<byte[]> GenerateOfferLetterPdfAsync(OfferLetterDocument document) => Task.FromResult(new byte[] { 1, 2, 3 });
        }

        private class FakeFileStorageService : IFileStorageService
        {
            public Task<string> SaveFileAsync(string container, string path, byte[] content, string contentType) => Task.FromResult(path);
            public Task<byte[]?> GetFileAsync(string container, string path) => Task.FromResult<byte[]?>(new byte[] { 1, 2, 3 });
            public Task DeleteFileAsync(string container, string path) => Task.CompletedTask;
        }

        private static ApplicationDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("ems_sweep_test_" + Guid.NewGuid())
                .Options;
            return new ApplicationDbContext(options);
        }

        private static async Task<Designation> SeedDesignationAsync(ApplicationDbContext db, string name = "Designation")
        {
            var designation = new Designation { Id = Guid.NewGuid(), Name = name + "-" + Guid.NewGuid().ToString("N")[..8], Code = "DSG-" + Guid.NewGuid().ToString("N")[..8], CreatedAtUtc = DateTime.UtcNow };
            db.Designations.Add(designation);
            await db.SaveChangesAsync();
            return designation;
        }

        private static async Task<Employee> SeedEmployeeAsync(ApplicationDbContext db, Guid designationId)
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
                DesignationId = designationId,
                OfficeLocationId = officeLocationId
            };
            db.Employees.Add(employee);
            await db.SaveChangesAsync();
            return employee;
        }

        // ─── Offer expiry ──────────────────────────────────────────────────────────

        [Fact]
        public async Task RunDailySweep_ExpiresSentOfferPastExpiresAtUtc()
        {
            using var db = CreateDb();
            var designation = await SeedDesignationAsync(db);
            var recruitmentRepo = new RecruitmentRepository(db);
            var performanceRepo = new PerformanceRepository(db);
            var employeeRepo = new EmployeeRepository(db);

            var createCandidateHandler = new CreateCandidateCommandHandler(recruitmentRepo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<CreateCandidateCommandHandler>.Instance);
            var createOfferHandler = new CreateOfferCommandHandler(recruitmentRepo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<CreateOfferCommandHandler>.Instance);
            var sendHandler = new SendOfferCommandHandler(recruitmentRepo, new FakePdfService(), new FakeFileStorageService(), new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<SendOfferCommandHandler>.Instance);
            var sweepHandler = new RunDailySweepCommandHandler(recruitmentRepo, performanceRepo, employeeRepo, new RecordingAuditLogger(), NullLogger<RunDailySweepCommandHandler>.Instance);

            var candidateId = await createCandidateHandler.Handle(new CreateCandidateCommand
            {
                FirstName = "Jane",
                LastName = "Doe",
                Email = $"{Guid.NewGuid():N}@candidates.local",
                DesignationId = designation.Id,
                AppliedDate = DateTime.UtcNow.Date
            }, CancellationToken.None);

            var offerId = await createOfferHandler.Handle(new CreateOfferCommand
            {
                CandidateId = candidateId,
                DesignationId = designation.Id,
                OfferedSalary = 5000m,
                JoiningDate = DateTime.UtcNow.Date.AddDays(30),
                ExpiresAtUtc = DateTime.UtcNow.AddDays(5) // valid at creation time
            }, CancellationToken.None);

            await sendHandler.Handle(new SendOfferCommand { Id = offerId }, CancellationToken.None);

            // Backdate the expiry directly (the validator only allows a future date at creation time).
            var offer = await recruitmentRepo.GetOfferByIdAsync(offerId, CancellationToken.None);
            offer!.ExpiresAtUtc = DateTime.UtcNow.AddDays(-1);
            await recruitmentRepo.UpdateOfferAsync(offer, CancellationToken.None);
            await recruitmentRepo.SaveChangesAsync(CancellationToken.None);

            var result = await sweepHandler.Handle(new RunDailySweepCommand(), CancellationToken.None);

            Assert.Equal(1, result.OffersExpired);
            var expiredOffer = await recruitmentRepo.GetOfferByIdAsync(offerId, CancellationToken.None);
            Assert.Equal(OfferStatus.Expired, expiredOffer!.Status);
        }

        [Fact]
        public async Task RunDailySweep_IgnoresOfferWithNoExpiryOrFutureExpiry()
        {
            using var db = CreateDb();
            var designation = await SeedDesignationAsync(db);
            var recruitmentRepo = new RecruitmentRepository(db);
            var performanceRepo = new PerformanceRepository(db);
            var employeeRepo = new EmployeeRepository(db);

            var createCandidateHandler = new CreateCandidateCommandHandler(recruitmentRepo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<CreateCandidateCommandHandler>.Instance);
            var createOfferHandler = new CreateOfferCommandHandler(recruitmentRepo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<CreateOfferCommandHandler>.Instance);
            var sendHandler = new SendOfferCommandHandler(recruitmentRepo, new FakePdfService(), new FakeFileStorageService(), new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<SendOfferCommandHandler>.Instance);
            var sweepHandler = new RunDailySweepCommandHandler(recruitmentRepo, performanceRepo, employeeRepo, new RecordingAuditLogger(), NullLogger<RunDailySweepCommandHandler>.Instance);

            var candidateId = await createCandidateHandler.Handle(new CreateCandidateCommand
            {
                FirstName = "Jane",
                LastName = "Doe",
                Email = $"{Guid.NewGuid():N}@candidates.local",
                DesignationId = designation.Id,
                AppliedDate = DateTime.UtcNow.Date
            }, CancellationToken.None);

            // No ExpiresAtUtc supplied — should never auto-expire.
            var offerId = await createOfferHandler.Handle(new CreateOfferCommand
            {
                CandidateId = candidateId,
                DesignationId = designation.Id,
                OfferedSalary = 5000m,
                JoiningDate = DateTime.UtcNow.Date.AddDays(30)
            }, CancellationToken.None);
            await sendHandler.Handle(new SendOfferCommand { Id = offerId }, CancellationToken.None);

            var result = await sweepHandler.Handle(new RunDailySweepCommand(), CancellationToken.None);

            Assert.Equal(0, result.OffersExpired);
            var offer = await recruitmentRepo.GetOfferByIdAsync(offerId, CancellationToken.None);
            Assert.Equal(OfferStatus.Sent, offer!.Status);
        }

        // ─── Promotions ────────────────────────────────────────────────────────────

        [Fact]
        public async Task ApprovePromotion_WithFutureEffectiveDate_DefersApplicationToEmployee()
        {
            using var db = CreateDb();
            var fromDesignation = await SeedDesignationAsync(db, "Engineer");
            var toDesignation = await SeedDesignationAsync(db, "Senior Engineer");
            var employee = await SeedEmployeeAsync(db, fromDesignation.Id);

            var performanceRepo = new PerformanceRepository(db);
            var authRepo = new AuthRepository(db);
            var employeeRepo = new EmployeeRepository(db);
            var proposeHandler = new ProposePromotionCommandHandler(performanceRepo, authRepo, employeeRepo, new RecordingAuditLogger(), NullLogger<ProposePromotionCommandHandler>.Instance);
            var approveHandler = new ApprovePromotionCommandHandler(performanceRepo, employeeRepo, new RecordingAuditLogger(), NullLogger<ApprovePromotionCommandHandler>.Instance);

            var promotionId = await proposeHandler.Handle(new ProposePromotionCommand
            {
                EmployeeId = employee.Id,
                ToDesignationId = toDesignation.Id,
                EffectiveDate = DateTime.UtcNow.AddDays(30), // future
                Reason = "Deferred promotion test.",
                RequestingUserId = Guid.NewGuid(),
                IsPrivileged = true
            }, CancellationToken.None);

            await approveHandler.Handle(new ApprovePromotionCommand { Id = promotionId, RequestingUserId = Guid.NewGuid() }, CancellationToken.None);

            var promotion = await performanceRepo.GetPromotionByIdAsync(promotionId, CancellationToken.None);
            Assert.Equal(PromotionStatus.Approved, promotion!.Status);
            Assert.Null(promotion.AppliedAtUtc);

            var stillUnchangedEmployee = await employeeRepo.GetByIdAsync(employee.Id, CancellationToken.None);
            Assert.Equal(fromDesignation.Id, stillUnchangedEmployee!.DesignationId);
        }

        [Fact]
        public async Task RunDailySweep_AppliesApprovedPromotionOnceEffectiveDateArrives()
        {
            using var db = CreateDb();
            var fromDesignation = await SeedDesignationAsync(db, "Engineer");
            var toDesignation = await SeedDesignationAsync(db, "Senior Engineer");
            var employee = await SeedEmployeeAsync(db, fromDesignation.Id);

            var promotion = new Promotion
            {
                Id = Guid.NewGuid(),
                PromotionNumber = "PRO-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
                EmployeeId = employee.Id,
                FromDesignationId = fromDesignation.Id,
                ToDesignationId = toDesignation.Id,
                EffectiveDate = DateTime.UtcNow.AddDays(-1), // now due
                Reason = "Deferred promotion, now due.",
                Status = PromotionStatus.Approved,
                ProposedByUserId = Guid.NewGuid(),
                DecidedByUserId = Guid.NewGuid(),
                DecidedAtUtc = DateTime.UtcNow.AddDays(-10),
                AppliedAtUtc = null, // approved earlier, application deferred until now
                CreatedAtUtc = DateTime.UtcNow.AddDays(-10),
                IsDeleted = false
            };
            db.Promotions.Add(promotion);
            await db.SaveChangesAsync();

            var recruitmentRepo = new RecruitmentRepository(db);
            var performanceRepo = new PerformanceRepository(db);
            var employeeRepo = new EmployeeRepository(db);
            var sweepHandler = new RunDailySweepCommandHandler(recruitmentRepo, performanceRepo, employeeRepo, new RecordingAuditLogger(), NullLogger<RunDailySweepCommandHandler>.Instance);

            var result = await sweepHandler.Handle(new RunDailySweepCommand(), CancellationToken.None);

            Assert.Equal(1, result.PromotionsApplied);

            var appliedPromotion = await performanceRepo.GetPromotionByIdAsync(promotion.Id, CancellationToken.None);
            Assert.NotNull(appliedPromotion!.AppliedAtUtc);

            var updatedEmployee = await employeeRepo.GetByIdAsync(employee.Id, CancellationToken.None);
            Assert.Equal(toDesignation.Id, updatedEmployee!.DesignationId);
        }

        [Fact]
        public async Task RunDailySweep_IgnoresApprovedPromotionNotYetDue()
        {
            using var db = CreateDb();
            var fromDesignation = await SeedDesignationAsync(db, "Engineer");
            var toDesignation = await SeedDesignationAsync(db, "Senior Engineer");
            var employee = await SeedEmployeeAsync(db, fromDesignation.Id);

            var promotion = new Promotion
            {
                Id = Guid.NewGuid(),
                PromotionNumber = "PRO-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
                EmployeeId = employee.Id,
                FromDesignationId = fromDesignation.Id,
                ToDesignationId = toDesignation.Id,
                EffectiveDate = DateTime.UtcNow.AddDays(30), // not due yet
                Reason = "Not due yet.",
                Status = PromotionStatus.Approved,
                ProposedByUserId = Guid.NewGuid(),
                DecidedByUserId = Guid.NewGuid(),
                DecidedAtUtc = DateTime.UtcNow,
                AppliedAtUtc = null,
                CreatedAtUtc = DateTime.UtcNow,
                IsDeleted = false
            };
            db.Promotions.Add(promotion);
            await db.SaveChangesAsync();

            var recruitmentRepo = new RecruitmentRepository(db);
            var performanceRepo = new PerformanceRepository(db);
            var employeeRepo = new EmployeeRepository(db);
            var sweepHandler = new RunDailySweepCommandHandler(recruitmentRepo, performanceRepo, employeeRepo, new RecordingAuditLogger(), NullLogger<RunDailySweepCommandHandler>.Instance);

            var result = await sweepHandler.Handle(new RunDailySweepCommand(), CancellationToken.None);

            Assert.Equal(0, result.PromotionsApplied);
            var unchangedEmployee = await employeeRepo.GetByIdAsync(employee.Id, CancellationToken.None);
            Assert.Equal(fromDesignation.Id, unchangedEmployee!.DesignationId);
        }
    }
}
