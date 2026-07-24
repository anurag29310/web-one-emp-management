using MediatR;
using System;

namespace EMS.Application.Features.OfficeLocations
{
    public class DeleteOfficeLocationCommand : IRequest
    {
        public Guid Id { get; set; }
    }
}
