using EMS.Domain.Entities;
using MediatR;
using System;

namespace EMS.Application.Features.Designations
{
    public class GetDesignationByIdQuery : IRequest<Designation?>
    {
        public Guid Id { get; set; }
    }
}
