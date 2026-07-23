using EMS.Application.DTOs;
using MediatR;

namespace EMS.Application.Features.Health.Queries
{
    public class GetReadinessQuery : IRequest<ReadinessStatusDto>
    {
    }
}
