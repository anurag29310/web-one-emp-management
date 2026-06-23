using MediatR;

namespace EMS.Application.Features.Leave.Queries
{
    public class GetHolidaysQuery : IRequest<System.Collections.Generic.IEnumerable<EMS.Domain.Entities.Holiday>>
    {
        public int Year { get; set; }
    }
}
