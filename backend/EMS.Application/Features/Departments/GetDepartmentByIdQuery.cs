using EMS.Domain.Entities;
using MediatR;
using System;

namespace EMS.Application.Features.Departments
{
    public class GetDepartmentByIdQuery : IRequest<Department?>
    {
        public Guid Id { get; set; }
    }
}
