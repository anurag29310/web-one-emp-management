using EMS.Application.Features.Recruitment.Commands;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Recruitment.Handlers
{
    public class AddChecklistItemCommandHandler : IRequestHandler<AddChecklistItemCommand, Guid>
    {
        private readonly IRecruitmentRepository _repo;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<AddChecklistItemCommandHandler> _logger;

        public AddChecklistItemCommandHandler(IRecruitmentRepository repo, ICurrentUserService currentUser, ILogger<AddChecklistItemCommandHandler> logger)
        {
            _repo = repo;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<Guid> Handle(AddChecklistItemCommand request, CancellationToken cancellationToken)
        {
            var candidate = await _repo.GetByIdAsync(request.CandidateId, cancellationToken)
                ?? throw new InvalidOperationException($"Candidate {request.CandidateId} not found.");

            var item = new OnboardingChecklistItem
            {
                Id = Guid.NewGuid(),
                CandidateId = candidate.Id,
                ItemName = request.ItemName,
                Notes = request.Notes,
                IsCompleted = false,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedBy = _currentUser.UserId
            };

            await _repo.AddChecklistItemAsync(item, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Added checklist item {ItemId} for candidate {CandidateId}", item.Id, candidate.Id);

            return item.Id;
        }
    }
}
