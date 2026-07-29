using EMS.Application.Features.Recruitment.Commands;
using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Recruitment.Handlers
{
    public class CompleteChecklistItemCommandHandler : IRequestHandler<CompleteChecklistItemCommand>
    {
        private readonly IRecruitmentRepository _repo;
        private readonly ILogger<CompleteChecklistItemCommandHandler> _logger;

        public CompleteChecklistItemCommandHandler(IRecruitmentRepository repo, ILogger<CompleteChecklistItemCommandHandler> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task Handle(CompleteChecklistItemCommand request, CancellationToken cancellationToken)
        {
            var item = await _repo.GetChecklistItemByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Checklist item {request.Id} not found.");

            item.IsCompleted = true;
            item.CompletedAtUtc = DateTime.UtcNow;
            item.CompletedBy = request.RequestingUserId;
            if (!string.IsNullOrWhiteSpace(request.Notes))
                item.Notes = request.Notes;

            await _repo.UpdateChecklistItemAsync(item, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Completed checklist item {ItemId}", item.Id);
        }
    }
}
