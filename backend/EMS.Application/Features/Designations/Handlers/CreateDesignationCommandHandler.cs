using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Designations.Handlers
{
    public class CreateDesignationCommandHandler : IRequestHandler<CreateDesignationCommand, Designation>
    {
        private readonly IDesignationRepository _repo;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<CreateDesignationCommandHandler> _logger;

        public CreateDesignationCommandHandler(IDesignationRepository repo, ICurrentUserService currentUser, ILogger<CreateDesignationCommandHandler> logger)
        {
            _repo = repo;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<Designation> Handle(CreateDesignationCommand request, CancellationToken cancellationToken)
        {
            if (await _repo.NameExistsAsync(request.Name, ct: cancellationToken))
                throw new InvalidOperationException("Designation name already exists.");
            if (await _repo.CodeExistsAsync(request.Code, ct: cancellationToken))
                throw new InvalidOperationException("Designation code already exists.");

            var designation = new Designation
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Code = request.Code,
                Level = request.Level,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedBy = _currentUser.UserId,
                IsDeleted = false
            };

            await _repo.AddAsync(designation, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Created designation {DesignationName} ({DesignationId})", designation.Name, designation.Id);
            return designation;
        }
    }
}
