using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Designations.Handlers
{
    public class UpdateDesignationCommandHandler : IRequestHandler<UpdateDesignationCommand, Designation>
    {
        private readonly IDesignationRepository _repo;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<UpdateDesignationCommandHandler> _logger;

        public UpdateDesignationCommandHandler(IDesignationRepository repo, ICurrentUserService currentUser, ILogger<UpdateDesignationCommandHandler> logger)
        {
            _repo = repo;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<Designation> Handle(UpdateDesignationCommand request, CancellationToken cancellationToken)
        {
            var designation = await _repo.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Designation {request.Id} not found.");

            if (await _repo.NameExistsAsync(request.Name, request.Id, cancellationToken))
                throw new InvalidOperationException("Designation name already exists.");
            if (await _repo.CodeExistsAsync(request.Code, request.Id, cancellationToken))
                throw new InvalidOperationException("Designation code already exists.");

            designation.Name = request.Name;
            designation.Code = request.Code;
            designation.Level = request.Level;
            designation.UpdatedAtUtc = DateTime.UtcNow;
            designation.UpdatedBy = _currentUser.UserId;

            await _repo.UpdateAsync(designation, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Updated designation {DesignationId}", designation.Id);
            return designation;
        }
    }
}
