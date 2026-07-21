using EMS.Application.Features.Payroll.Dtos;
using MediatR;
using System;

namespace EMS.Application.Features.Payroll.Queries
{
    public class GetPayrollRunByIdQuery : IRequest<PayrollRunDto?>
    {
        public Guid Id { get; set; }
    }
}
