using EMS.Domain.Entities;
using MediatR;
using System;

namespace EMS.Application.Features.OfficeLocations
{
    public class UpdateOfficeLocationCommand : IRequest<OfficeLocation>
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Code { get; set; } = null!;
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string City { get; set; } = null!;
        public string? State { get; set; }
        public string Country { get; set; } = null!;
        public string TimeZoneId { get; set; } = null!;
    }
}
