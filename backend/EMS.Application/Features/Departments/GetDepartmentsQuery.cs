using EMS.Domain.Entities;
using MediatR;
using System.Collections.Generic;

namespace EMS.Application.Features.Departments
{
    public class GetDepartmentsQuery : IRequest<IEnumerable<Department>>
    {
    }
}
