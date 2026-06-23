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
        private readonly ILogger<UpdateEmployeeCommandHandler> _logger;

        public UpdateEmployeeCommandHandler(IEmployeeRepository repo, ILogger<UpdateEmployeeCommandHandler> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<Employee> Handle(Commands.UpdateEmployeeCommand request, CancellationToken cancellationToken)
        {
            var emp = await _repo.GetByIdAsync(request.Id) ?? throw new InvalidOperationException("Employee not found");

            if (!string.IsNullOrWhiteSpace(request.Email) && await _repo.EmailExistsAsync(request.Email, request.Id))
                throw new InvalidOperationException("Email already exists");
            if (await _repo.EmployeeCodeExistsAsync(request.EmployeeCode, request.Id))
                throw new InvalidOperationException("Employee code already exists");

            emp.EmployeeCode = request.EmployeeCode;
            emp.FirstName = request.FirstName;
            emp.LastName = request.LastName;
            emp.Email = request.Email;
            emp.PhoneNumber = request.PhoneNumber;
            emp.DateOfBirth = request.DateOfBirth;
            emp.Gender = request.Gender;
            emp.Address = request.Address;
            emp.EmergencyContactName = request.EmergencyContactName;
            emp.EmergencyContactNumber = request.EmergencyContactNumber;
            emp.JoinDate = request.JoinDate;
            emp.DepartmentId = request.DepartmentId;
            emp.Designation = request.Designation;
            emp.ManagerId = request.ManagerId;
            emp.ProfilePhotoUrl = request.ProfilePhotoUrl;
            emp.EmploymentStatus = request.EmploymentStatus;

            await _repo.UpdateAsync(emp);
            await _repo.SaveChangesAsync();
            _logger.LogInformation("Updated employee {EmployeeId}", emp.Id);
            return emp;
        }
    }
}
