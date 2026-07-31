using EMS.Domain.Entities;
using MediatR;
using System;

namespace EMS.Application.Features.Companies.Commands
{
    public class UpdateCompanyCommand : IRequest<Company>
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Timezone { get; set; } = "UTC";
        public string Currency { get; set; } = "USD";
        public string? LogoUrl { get; set; }
    }
}
