using MediatR;
using System;

namespace EMS.Application.Features.Leave.Queries
{
    public class GetHolidayByIdQuery : IRequest<EMS.Domain.Entities.Holiday?>
    {
        public Guid Id { get; set; }
    }
}
