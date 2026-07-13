using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Leave.Handlers
{
    public class CreateHolidayCommandHandler : IRequestHandler<Commands.CreateHolidayCommand, Holiday>
    {
        private readonly ILeaveRepository _repo;
        private readonly ILogger<CreateHolidayCommandHandler> _logger;

        public CreateHolidayCommandHandler(ILeaveRepository repo, ILogger<CreateHolidayCommandHandler> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<Holiday> Handle(Commands.CreateHolidayCommand request, CancellationToken cancellationToken)
        {
            var holiday = new Holiday
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                OfficeLocationId = request.OfficeLocationId,
                HolidayDate = request.HolidayDate.Date,
                IsOptional = request.IsOptional,
                CreatedAtUtc = DateTime.UtcNow,
                IsDeleted = false
            };

            await _repo.AddHolidayAsync(holiday, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Created holiday {HolidayName} ({HolidayId})", holiday.Name, holiday.Id);
            return holiday;
        }
    }
}
