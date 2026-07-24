using EMS.Domain.Entities;
using MediatR;

namespace EMS.Application.Features.Designations
{
    public class CreateDesignationCommand : IRequest<Designation>
    {
        public string Name { get; set; } = null!;
        public string Code { get; set; } = null!;
        public int? Level { get; set; }
    }
}
