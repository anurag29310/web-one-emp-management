using EMS.Application.Features.Employees.Commands;
using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Employees.Handlers
{
    public class ActivateEmployeeCommandHandler : IRequestHandler<Commands.ActivateEmployeeCommand>
    {
        private readonly IEmployeeRepository _repo;
        private readonly IAuditLogger _auditLogger;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<ActivateEmployeeCommandHandler> _logger;

        public ActivateEmployeeCommandHandler(IEmployeeRepository repo, IAuditLogger auditLogger, ICurrentUserService currentUser, ILogger<ActivateEmployeeCommandHandler> logger)
        {
            _repo = repo;
            _auditLogger = auditLogger;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<Unit> Handle(Commands.ActivateEmployeeCommand request, CancellationToken cancellationToken)
        {
            var emp = await _repo.GetByIdIncludingDeletedAsync(request.Id, cancellationToken);
            if (emp == null || emp.CompanyId != _currentUser.CompanyId!.Value)
                throw new System.InvalidOperationException($"Employee {request.Id} not found.");
            emp.IsActive = true;
            emp.UpdatedAtUtc = System.DateTime.UtcNow;
            await _repo.UpdateAsync(emp, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Activated employee {EmployeeId}", emp.Id);

            await _auditLogger.LogAsync("Employee", emp.Id, "Activated", ct: cancellationToken);

            return Unit.Value;
        }

        Task IRequestHandler<ActivateEmployeeCommand>.Handle(ActivateEmployeeCommand request, CancellationToken cancellationToken)
        {
            return Handle(request, cancellationToken);
        }
    }
}
