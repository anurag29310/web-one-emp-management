using EMS.Application.Features.Employees.Commands;
using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EMS.Application.Features.Employees.Handlers
{
    public class DeactivateEmployeeCommandHandler : IRequestHandler<Commands.DeactivateEmployeeCommand>
    {
        private readonly IEmployeeRepository _repo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<DeactivateEmployeeCommandHandler> _logger;

        public DeactivateEmployeeCommandHandler(IEmployeeRepository repo, IAuditLogger auditLogger, ILogger<DeactivateEmployeeCommandHandler> logger)
        {
            _repo = repo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task<Unit> Handle(Commands.DeactivateEmployeeCommand request, CancellationToken cancellationToken)
        {
            var emp = await _repo.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new System.InvalidOperationException($"Employee {request.Id} not found.");
            emp.IsActive = false;
            await _repo.UpdateAsync(emp, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Deactivated employee {EmployeeId}", emp.Id);

            await _auditLogger.LogAsync("Employee", emp.Id, "Deactivated", ct: cancellationToken);

            return Unit.Value;
        }

        Task IRequestHandler<DeactivateEmployeeCommand>.Handle(DeactivateEmployeeCommand request, CancellationToken cancellationToken)
        {
            return Handle(request, cancellationToken);
        }
    }
}
