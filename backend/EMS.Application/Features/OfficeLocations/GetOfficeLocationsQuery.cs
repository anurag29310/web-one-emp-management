using EMS.Domain.Entities;
using MediatR;
using System.Collections.Generic;

namespace EMS.Application.Features.OfficeLocations
{
    public class GetOfficeLocationsQuery : IRequest<IEnumerable<OfficeLocation>>
    {
    }
}
