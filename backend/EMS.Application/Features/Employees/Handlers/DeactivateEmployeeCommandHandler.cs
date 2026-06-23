using EMS.Application.Features.Employees.Commands;
using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EMS.Application.Features.Employees.Handlers
{
    public class DeactivateEmployeeCommandHandler : IRequestHandler<Commands.DeactivateEmployeeCommand>
    {
        private readonly IEmployeeRepository _repo;
        private readonly ILogger<DeactivateEmployeeCommandHandler> _logger;

        public DeactivateEmployeeCommandHandler(IEmployeeRepository repo, ILogger<DeactivateEmployeeCommandHandler> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<Unit> Handle(Commands.DeactivateEmployeeCommand request, CancellationToken cancellationToken)
        {
            var emp = await _repo.GetByIdAsync(request.Id) ?? throw new System.InvalidOperationException("Employee not found");
            emp.IsActive = false;
            await _repo.UpdateAsync(emp);
            await _repo.SaveChangesAsync();
            _logger.LogInformation("Deactivated employee {EmployeeId}", emp.Id);
            return Unit.Value;
        }

        Task IRequestHandler<DeactivateEmployeeCommand>.Handle(DeactivateEmployeeCommand request, CancellationToken cancellationToken)
        {
            return Handle(request, cancellationToken);
        }
    }
}
