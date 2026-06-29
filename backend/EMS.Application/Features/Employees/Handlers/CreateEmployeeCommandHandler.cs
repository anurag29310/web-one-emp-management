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
        private readonly ILogger<CreateEmployeeCommandHandler> _logger;

        public CreateEmployeeCommandHandler(IEmployeeRepository repo, ILogger<CreateEmployeeCommandHandler> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<Employee> Handle(Commands.CreateEmployeeCommand request, CancellationToken cancellationToken)
        {
            if (await _repo.EmployeeCodeExistsAsync(request.EmployeeCode, ct: cancellationToken))
                throw new InvalidOperationException("Employee code already exists.");
            if (!string.IsNullOrWhiteSpace(request.Email) && await _repo.EmailExistsAsync(request.Email, ct: cancellationToken))
                throw new InvalidOperationException("Email already exists.");

            var emp = new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeCode = request.EmployeeCode,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                DateOfBirth = request.DateOfBirth,
                Gender = request.Gender,
                Address = request.Address,
                EmergencyContactName = request.EmergencyContactName,
                EmergencyContactNumber = request.EmergencyContactNumber,
                JoinDate = request.JoinDate,
                DepartmentId = request.DepartmentId,
                Designation = request.Designation,
                ManagerId = request.ManagerId,
                ProfilePhotoUrl = request.ProfilePhotoUrl,
                EmploymentStatus = request.EmploymentStatus,
                IsActive = true
            };

            await _repo.AddAsync(emp, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Created employee {EmployeeCode}", emp.EmployeeCode);
            return emp;
        }
    }
}
