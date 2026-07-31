using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Departments.Handlers
{
    public class DeleteDepartmentCommandHandler : IRequestHandler<DeleteDepartmentCommand>
    {
        private readonly IDepartmentRepository _repo;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<DeleteDepartmentCommandHandler> _logger;

        public DeleteDepartmentCommandHandler(IDepartmentRepository repo, ICurrentUserService currentUser, ILogger<DeleteDepartmentCommandHandler> logger)
        {
            _repo = repo;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task Handle(DeleteDepartmentCommand request, CancellationToken cancellationToken)
        {
            var dept = await _repo.GetByIdAsync(request.Id, _currentUser.CompanyId!.Value, cancellationToken)
                ?? throw new InvalidOperationException($"Department {request.Id} not found.");

            await _repo.DeleteAsync(dept, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Deleted (soft) department {DepartmentId}", dept.Id);
        }
    }
}
