using EMS.Domain.Entities;
using MediatR;
using System.Collections.Generic;

namespace EMS.Application.Features.Designations
{
    public class GetDesignationsQuery : IRequest<IEnumerable<Designation>>
    {
    }
}
