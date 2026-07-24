using EMS.Domain.Entities;
using MediatR;
using System;

namespace EMS.Application.Features.Designations
{
    public class UpdateDesignationCommand : IRequest<Designation>
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Code { get; set; } = null!;
        public int? Level { get; set; }
    }
}
