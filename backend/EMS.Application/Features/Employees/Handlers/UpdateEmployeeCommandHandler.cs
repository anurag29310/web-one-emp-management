using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Employees.Handlers
{
    public class UpdateEmployeeCommandHandler : IRequestHandler<Commands.UpdateEmployeeCommand, Employee>
    {
        private readonly IEmployeeRepository _repo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<UpdateEmployeeCommandHandler> _logger;

        public UpdateEmployeeCommandHandler(IEmployeeRepository repo, IAuditLogger auditLogger, ILogger<UpdateEmployeeCommandHandler> logger)
        {
            _repo = repo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task<Employee> Handle(Commands.UpdateEmployeeCommand request, CancellationToken cancellationToken)
        {
            var emp = await _repo.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Employee {request.Id} not found.");

            if (!string.IsNullOrWhiteSpace(request.Email) && await _repo.EmailExistsAsync(request.Email, request.Id, cancellationToken))
                throw new InvalidOperationException("Email already exists.");
            if (await _repo.EmployeeCodeExistsAsync(request.EmployeeCode, request.Id, cancellationToken))
                throw new InvalidOperationException("Employee code already exists.");

            var oldValues = new
            {
                emp.EmployeeCode,
                emp.FirstName,
                emp.LastName,
                emp.Email,
                emp.DepartmentId,
                emp.EmploymentStatus
            };

            emp.EmployeeCode = request.EmployeeCode;
            emp.FirstName = request.FirstName;
            emp.MiddleName = request.MiddleName;
            emp.LastName = request.LastName;
            emp.Email = request.Email;
            emp.PhoneNumber = request.PhoneNumber;
            emp.DateOfBirth = request.DateOfBirth;
            emp.Gender = request.Gender;
            emp.AddressLine1 = request.Address?.AddressLine1;
            emp.AddressLine2 = request.Address?.AddressLine2;
            emp.City = request.Address?.City;
            emp.State = request.Address?.State;
            emp.PostalCode = request.Address?.PostalCode;
            emp.Country = request.Address?.Country;
            emp.EmergencyContactName = request.EmergencyContact?.Name;
            emp.EmergencyContactPhone = request.EmergencyContact?.Phone;
            emp.EmergencyContactRelation = request.EmergencyContact?.Relation;
            emp.JoinDate = request.JoinDate;
            emp.DepartmentId = request.DepartmentId;
            emp.TeamId = request.TeamId;
            emp.DesignationId = request.DesignationId;
            emp.ManagerId = request.ManagerId;
            emp.OfficeLocationId = request.OfficeLocationId;
            emp.EmploymentStatus = request.EmploymentStatus;
            emp.UpdatedAtUtc = DateTime.UtcNow;

            await _repo.UpdateAsync(emp, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Updated employee {EmployeeId}", emp.Id);

            await _auditLogger.LogAsync("Employee", emp.Id, "Updated", oldValues: oldValues, newValues: new
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
