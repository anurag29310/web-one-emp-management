using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Employees.Handlers
{
    public class CreateEmployeeCommandHandler : IRequestHandler<Commands.CreateEmployeeCommand, Employee>
    {
        private readonly IEmployeeRepository _repo;
        private readonly IAuditLogger _auditLogger;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<CreateEmployeeCommandHandler> _logger;

        public CreateEmployeeCommandHandler(IEmployeeRepository repo, IAuditLogger auditLogger, ICurrentUserService currentUser, ILogger<CreateEmployeeCommandHandler> logger)
        {
            _repo = repo;
            _auditLogger = auditLogger;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<Employee> Handle(Commands.CreateEmployeeCommand request, CancellationToken cancellationToken)
        {
            var companyId = _currentUser.CompanyId!.Value;
            if (await _repo.EmployeeCodeExistsAsync(request.EmployeeCode, companyId, ct: cancellationToken))
                throw new InvalidOperationException("Employee code already exists.");
            if (!string.IsNullOrWhiteSpace(request.Email) && await _repo.EmailExistsAsync(request.Email, companyId, ct: cancellationToken))
                throw new InvalidOperationException("Email already exists.");

            var emp = new Employee
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                EmployeeCode = request.EmployeeCode,
                FirstName = request.FirstName,
                MiddleName = request.MiddleName,
                LastName = request.LastName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                DateOfBirth = request.DateOfBirth,
                Gender = request.Gender,
                AddressLine1 = request.Address?.AddressLine1,
                AddressLine2 = request.Address?.AddressLine2,
                City = request.Address?.City,
                State = request.Address?.State,
                PostalCode = request.Address?.PostalCode,
                Country = request.Address?.Country,
                EmergencyContactName = request.EmergencyContact?.Name,
                EmergencyContactPhone = request.EmergencyContact?.Phone,
                EmergencyContactRelation = request.EmergencyContact?.Relation,
                JoinDate = request.JoinDate,
                DepartmentId = request.DepartmentId,
                TeamId = request.TeamId,
                DesignationId = request.DesignationId,
                ManagerId = request.ManagerId,
                OfficeLocationId = request.OfficeLocationId,
                EmploymentStatus = request.EmploymentStatus,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            await _repo.AddAsync(emp, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Created employee {EmployeeCode}", emp.EmployeeCode);

            await _auditLogger.LogAsync("Employee", emp.Id, "Created", newValues: new
            {
                emp.EmployeeCode,
                emp.FirstName,
                emp.LastName,
                emp.Email,
                emp.DepartmentId,
                emp.EmploymentStatus
            }, ct: cancellationToken);

            return emp;
        }
    }
}
