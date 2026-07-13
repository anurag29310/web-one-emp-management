using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Leave.Handlers
{
    public class UpdateHolidayCommandHandler : IRequestHandler<Commands.UpdateHolidayCommand, Holiday>
    {
        private readonly ILeaveRepository _repo;
        private readonly ILogger<UpdateHolidayCommandHandler> _logger;

        public UpdateHolidayCommandHandler(ILeaveRepository repo, ILogger<UpdateHolidayCommandHandler> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<Holiday> Handle(Commands.UpdateHolidayCommand request, CancellationToken cancellationToken)
        {
            var holiday = await _repo.GetHolidayByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Holiday {request.Id} not found.");

            holiday.Name = request.Name;
            holiday.OfficeLocationId = request.OfficeLocationId;
            holiday.HolidayDate = request.HolidayDate.Date;
            holiday.IsOptional = request.IsOptional;
            holiday.UpdatedAtUtc = DateTime.UtcNow;

            await _repo.UpdateHolidayAsync(holiday, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Updated holiday {HolidayId}", holiday.Id);
            return holiday;
        }
    }
}
