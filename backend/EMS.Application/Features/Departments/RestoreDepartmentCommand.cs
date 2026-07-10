using MediatR;
using System;

namespace EMS.Application.Features.Departments
{
    public class RestoreDepartmentCommand : IRequest
    {
        public Guid Id { get; set; }
    }
}
