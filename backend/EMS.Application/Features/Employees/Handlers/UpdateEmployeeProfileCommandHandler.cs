using EMS.Application.Features.Employees.Commands;
using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Employees.Handlers
{
    public class UpdateEmployeeProfileCommandHandler : IRequestHandler<UpdateEmployeeProfileCommand>
    {
        private readonly IEmployeeRepository _repo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<UpdateEmployeeProfileCommandHandler> _logger;

        public UpdateEmployeeProfileCommandHandler(IEmployeeRepository repo, IAuditLogger auditLogger, ILogger<UpdateEmployeeProfileCommandHandler> logger)
        {
            _repo = repo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(UpdateEmployeeProfileCommand request, CancellationToken cancellationToken)
        {
            var emp = await _repo.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Employee {request.Id} not found.");

            var oldValues = new
            {
                emp.PhoneNumber,
                emp.Address,
                emp.EmergencyContactName,
                emp.EmergencyContactNumber
            };

            emp.PhoneNumber = request.PhoneNumber;
            emp.Address = request.Address;
            emp.EmergencyContactName = request.EmergencyContactName;
            emp.EmergencyContactNumber = request.EmergencyContactNumber;

            await _repo.UpdateAsync(emp, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Employee {EmployeeId} self-service profile updated", emp.Id);

            await _auditLogger.LogAsync("Employee", emp.Id, "Updated", oldValues: oldValues, newValues: new
            {
                emp.PhoneNumber,
                emp.Address,
                emp.EmergencyContactName,
                emp.EmergencyContactNumber
            }, ct: cancellationToken);
        }
    }
}
