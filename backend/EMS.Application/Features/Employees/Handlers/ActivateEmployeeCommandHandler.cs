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
        private readonly ILogger<ActivateEmployeeCommandHandler> _logger;

        public ActivateEmployeeCommandHandler(IEmployeeRepository repo, ILogger<ActivateEmployeeCommandHandler> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<Unit> Handle(Commands.ActivateEmployeeCommand request, CancellationToken cancellationToken)
        {
            var emp = await _repo.GetByIdAsync(request.Id) ?? throw new System.InvalidOperationException("Employee not found");
            emp.IsActive = true;
            await _repo.UpdateAsync(emp);
            await _repo.SaveChangesAsync();
            _logger.LogInformation("Activated employee {EmployeeId}", emp.Id);
            return Unit.Value;
        }

        Task IRequestHandler<ActivateEmployeeCommand>.Handle(ActivateEmployeeCommand request, CancellationToken cancellationToken)
        {
            return Handle(request, cancellationToken);
        }
    }
}
