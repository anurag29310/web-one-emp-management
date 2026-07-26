using EMS.Application.Common.DTOs;
using EMS.Application.Features.Clients.DTOs;
using MediatR;

namespace EMS.Application.Features.Clients
{
    public class GetClientsQuery : IRequest<PagedResult<ClientDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Search { get; set; }
        public bool? IsActive { get; set; }
    }
}
