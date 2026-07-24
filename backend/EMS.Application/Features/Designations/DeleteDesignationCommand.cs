using MediatR;
using System;

namespace EMS.Application.Features.Designations
{
    public class DeleteDesignationCommand : IRequest
    {
        public Guid Id { get; set; }
    }
}
