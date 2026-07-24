using EMS.Domain.Entities;
using MediatR;
using System;

namespace EMS.Application.Features.OfficeLocations
{
    public class GetOfficeLocationByIdQuery : IRequest<OfficeLocation?>
    {
        public Guid Id { get; set; }
    }
}
