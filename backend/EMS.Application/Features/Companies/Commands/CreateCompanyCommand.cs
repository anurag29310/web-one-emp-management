using EMS.Domain.Entities;
using MediatR;

namespace EMS.Application.Features.Companies.Commands
{
    /// <summary>Super Admin directly creates a company (no approval gate — that's only for public
    /// self-registration via RegisterCompanyCommand). Lands straight in Active status.</summary>
    public class CreateCompanyCommand : IRequest<Company>
    {
        public string Name { get; set; } = null!;
        public string Timezone { get; set; } = "UTC";
        public string Currency { get; set; } = "USD";
        public string? LogoUrl { get; set; }
    }
}
