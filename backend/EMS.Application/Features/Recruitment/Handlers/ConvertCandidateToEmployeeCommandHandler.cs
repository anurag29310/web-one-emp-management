using EMS.Application.Features.Recruitment.Commands;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Recruitment.Handlers
{
    public class ConvertCandidateToEmployeeCommandHandler : IRequestHandler<ConvertCandidateToEmployeeCommand, Guid>
    {
        private readonly IRecruitmentRepository _recruitmentRepo;
        private readonly IEmployeeRepository _employeeRepo;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<ConvertCandidateToEmployeeCommandHandler> _logger;

        public ConvertCandidateToEmployeeCommandHandler(IRecruitmentRepository recruitmentRepo, IEmployeeRepository employeeRepo, ICurrentUserService currentUser, IAuditLogger auditLogger, ILogger<ConvertCandidateToEmployeeCommandHandler> logger)
        {
            _recruitmentRepo = recruitmentRepo;
            _employeeRepo = employeeRepo;
            _currentUser = currentUser;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task<Guid> Handle(ConvertCandidateToEmployeeCommand request, CancellationToken cancellationToken)
        {
            var candidate = await _recruitmentRepo.GetByIdAsync(request.CandidateId, cancellationToken)
                ?? throw new InvalidOperationException($"Candidate {request.CandidateId} not found.");

            if (candidate.ConvertedEmployeeId != null || candidate.Status == CandidateStatus.Hired)
                throw new InvalidOperationException($"Candidate {candidate.CandidateNumber} has already been converted to an employee.");

            var offers = await _recruitmentRepo.GetOffersByCandidateAsync(request.CandidateId, cancellationToken);
            var acceptedOffer = offers.FirstOrDefault(o => o.Status == OfferStatus.Accepted)
                ?? throw new InvalidOperationException($"Candidate {candidate.CandidateNumber} has no Accepted offer to convert from.");

            if (await _employeeRepo.EmployeeCodeExistsAsync(request.EmployeeCode, ct: cancellationToken))
                throw new InvalidOperationException("Employee code already exists.");
            if (await _employeeRepo.EmailExistsAsync(candidate.Email, ct: cancellationToken))
                throw new InvalidOperationException("Email already exists.");

            var employee = new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeCode = request.EmployeeCode,
                FirstName = candidate.FirstName,
                LastName = candidate.LastName,
                Email = candidate.Email,
                PhoneNumber = candidate.PhoneNumber,
                JoinDate = request.JoinDate ?? acceptedOffer.JoiningDate,
                DepartmentId = acceptedOffer.DepartmentId,
                TeamId = request.TeamId,
                DesignationId = acceptedOffer.DesignationId,
                ManagerId = request.ManagerId,
                OfficeLocationId = request.OfficeLocationId,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            await _employeeRepo.AddAsync(employee, cancellationToken);
            await _employeeRepo.SaveChangesAsync(cancellationToken);

            var now = DateTime.UtcNow;
            candidate.Status = CandidateStatus.Hired;
            candidate.ConvertedEmployeeId = employee.Id;
            candidate.UpdatedAtUtc = now;
            candidate.UpdatedBy = _currentUser.UserId;
            await _recruitmentRepo.UpdateAsync(candidate, cancellationToken);
            await _recruitmentRepo.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Converted candidate {CandidateId} to employee {EmployeeId} ({EmployeeCode})", candidate.Id, employee.Id, employee.EmployeeCode);

            await _auditLogger.LogAsync("Candidate", candidate.Id, "ConvertedToEmployee", newValues: new { EmployeeId = employee.Id, employee.EmployeeCode }, ct: cancellationToken);

            return employee.Id;
        }
    }
}
