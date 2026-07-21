using EMS.Application.Features.Employees.Commands;
using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Employees.Handlers
{
    public class UpdateEmployeeStatusCommandHandler : IRequestHandler<UpdateEmployeeStatusCommand>
    {
        private readonly IEmployeeRepository _repo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<UpdateEmployeeStatusCommandHandler> _logger;

        public UpdateEmployeeStatusCommandHandler(IEmployeeRepository repo, IAuditLogger auditLogger, ILogger<UpdateEmployeeStatusCommandHandler> logger)
        {
            _repo = repo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(UpdateEmployeeStatusCommand request, CancellationToken cancellationToken)
        {
            var emp = await _repo.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Employee {request.Id} not found.");

            var previousStatus = emp.EmploymentStatus;

            emp.EmploymentStatus = request.Status;
            emp.ExitDate = request.ExitDate;

            var isTerminated = string.Equals(request.Status, "Inactive", StringComparison.OrdinalIgnoreCase)
                || string.Equals(request.Status, "Terminated", StringComparison.OrdinalIgnoreCase);

            emp.IsActive = !isTerminated;

            await _repo.UpdateAsync(emp, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Employee {EmployeeId} status updated to {Status}", emp.Id, request.Status);

            await _auditLogger.LogAsync("Employee", emp.Id, "StatusChanged",
                oldValues: new { Status = previousStatus },
                newValues: new { Status = request.Status, request.ExitDate, request.Reason },
                ct: cancellationToken);
        }
    }
}
