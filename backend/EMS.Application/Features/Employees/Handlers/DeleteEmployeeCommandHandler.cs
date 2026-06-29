using EMS.Application.Features.Employees.Commands;
using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Employees.Handlers
{
    public class DeleteEmployeeCommandHandler : IRequestHandler<Commands.DeleteEmployeeCommand>
    {
        private readonly IEmployeeRepository _repo;
        private readonly ILogger<DeleteEmployeeCommandHandler> _logger;

        public DeleteEmployeeCommandHandler(IEmployeeRepository repo, ILogger<DeleteEmployeeCommandHandler> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<Unit> Handle(Commands.DeleteEmployeeCommand request, CancellationToken cancellationToken)
        {
            var emp = await _repo.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new System.InvalidOperationException($"Employee {request.Id} not found.");
            await _repo.DeleteAsync(emp, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Deleted (deactivated) employee {EmployeeId}", emp.Id);
            return Unit.Value;
        }

        Task IRequestHandler<DeleteEmployeeCommand>.Handle(DeleteEmployeeCommand request, CancellationToken cancellationToken)
        {
            return Handle(request, cancellationToken);
        }
    }
}
