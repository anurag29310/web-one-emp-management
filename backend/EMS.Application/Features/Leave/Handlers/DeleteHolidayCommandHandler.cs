using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Leave.Handlers
{
    public class DeleteHolidayCommandHandler : IRequestHandler<Commands.DeleteHolidayCommand>
    {
        private readonly ILeaveRepository _repo;
        private readonly ILogger<DeleteHolidayCommandHandler> _logger;

        public DeleteHolidayCommandHandler(ILeaveRepository repo, ILogger<DeleteHolidayCommandHandler> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task Handle(Commands.DeleteHolidayCommand request, CancellationToken cancellationToken)
        {
            var holiday = await _repo.GetHolidayByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Holiday {request.Id} not found.");

            await _repo.DeleteHolidayAsync(holiday, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Deleted (soft) holiday {HolidayId}", holiday.Id);
        }
    }
}
