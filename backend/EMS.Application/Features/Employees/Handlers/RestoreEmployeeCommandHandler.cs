using EMS.Application.Features.Employees.Commands;
using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Employees.Handlers
{
    public class RestoreEmployeeCommandHandler : IRequestHandler<RestoreEmployeeCommand>
    {
        private readonly IEmployeeRepository _repo;
        private readonly ILogger<RestoreEmployeeCommandHandler> _logger;

        public RestoreEmployeeCommandHandler(IEmployeeRepository repo, ILogger<RestoreEmployeeCommandHandler> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task Handle(RestoreEmployeeCommand request, CancellationToken cancellationToken)
        {
            var emp = await _repo.GetByIdIncludingDeletedAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Employee {request.Id} not found.");

            if (emp.IsActive)
                throw new InvalidOperationException("Employee is not deleted and cannot be restored.");

            await _repo.RestoreAsync(emp, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Employee {EmployeeId} restored", emp.Id);
        }
    }
}
