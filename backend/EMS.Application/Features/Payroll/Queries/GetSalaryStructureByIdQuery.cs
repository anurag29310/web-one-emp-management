using MediatR;
using System;
using EMS.Application.Features.Payroll.Dtos;

namespace EMS.Application.Features.Payroll.Queries
{
    public class GetSalaryStructureByIdQuery : IRequest<SalaryStructureDto?>
    {
        public Guid Id { get; set; }
    }
}
