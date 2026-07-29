using EMS.Domain.Entities;
using MediatR;
using System;

namespace EMS.Application.Features.Assets.Queries
{
    public class GetAssetByIdQuery : IRequest<Asset?>
    {
        public Guid Id { get; set; }
    }
}
