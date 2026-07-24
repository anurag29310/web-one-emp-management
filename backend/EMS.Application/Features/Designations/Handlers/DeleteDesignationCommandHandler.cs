using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Designations.Handlers
{
    public class DeleteDesignationCommandHandler : IRequestHandler<DeleteDesignationCommand>
    {
        private readonly IDesignationRepository _repo;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<DeleteDesignationCommandHandler> _logger;

        public DeleteDesignationCommandHandler(IDesignationRepository repo, ICurrentUserService currentUser, ILogger<DeleteDesignationCommandHandler> logger)
        {
            _repo = repo;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task Handle(DeleteDesignationCommand request, CancellationToken cancellationToken)
        {
            var designation = await _repo.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Designation {request.Id} not found.");

            designation.DeletedAtUtc = DateTime.UtcNow;
            designation.DeletedBy = _currentUser.UserId;

            await _repo.DeleteAsync(designation, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Deleted (soft) designation {DesignationId}", designation.Id);
        }
    }
}
