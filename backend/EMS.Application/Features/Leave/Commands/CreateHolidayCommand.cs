using MediatR;
using System;

namespace EMS.Application.Features.Leave.Commands
{
    public class CreateHolidayCommand : IRequest<EMS.Domain.Entities.Holiday>
    {
        public string Name { get; set; } = null!;
        public Guid? OfficeLocationId { get; set; }
        public DateTime HolidayDate { get; set; }
        public bool IsOptional { get; set; }
    }
}
