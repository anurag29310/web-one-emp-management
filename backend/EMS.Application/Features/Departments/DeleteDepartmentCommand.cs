using MediatR;
using System;

namespace EMS.Application.Features.Departments
{
    public class DeleteDepartmentCommand : IRequest
    {
        public Guid Id { get; set; }
    }
}
