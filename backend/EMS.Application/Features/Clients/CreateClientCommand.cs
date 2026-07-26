using EMS.Domain.Entities;
using MediatR;

namespace EMS.Application.Features.Clients
{
    public class CreateClientCommand : IRequest<Client>
    {
        public string ClientName { get; set; } = null!;
        public string CompanyName { get; set; } = null!;
        public string ContactPerson { get; set; } = null!;
        public string MobileNumber { get; set; } = null!;
        public string? AlternateMobile { get; set; }
        public string Email { get; set; } = null!;
        public string? GstNumber { get; set; }
        public string AddressLine1 { get; set; } = null!;
        public string? AddressLine2 { get; set; }
        public string City { get; set; } = null!;
        public string? State { get; set; }
        public string Country { get; set; } = null!;
        public string PostalCode { get; set; } = null!;
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? Notes { get; set; }
    }
}
