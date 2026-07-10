using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Departments.Handlers
{
    public class RestoreDepartmentCommandHandler : IRequestHandler<RestoreDepartmentCommand>
    {
        private readonly IDepartmentRepository _repo;
        private readonly ILogger<RestoreDepartmentCommandHandler> _logger;

        public RestoreDepartmentCommandHandler(IDepartmentRepository repo, ILogger<RestoreDepartmentCommandHandler> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task Handle(RestoreDepartmentCommand request, CancellationToken cancellationToken)
        {
            var dept = await _repo.GetByIdIncludingDeletedAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Department {request.Id} not found.");

            if (!dept.IsDeleted)
                throw new InvalidOperationException("Department is not deleted and cannot be restored.");

            await _repo.RestoreAsync(dept, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Restored department {DepartmentId}", dept.Id);
        }
    }
}
