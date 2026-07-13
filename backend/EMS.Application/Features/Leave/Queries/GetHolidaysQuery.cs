using MediatR;
using System;

namespace EMS.Application.Features.Leave.Queries
{
    public class GetHolidaysQuery : IRequest<System.Collections.Generic.IEnumerable<EMS.Domain.Entities.Holiday>>
    {
        public Guid? OfficeLocationId { get; set; }
        public int? Year { get; set; }
        public bool? IsOptional { get; set; }
    }
}
