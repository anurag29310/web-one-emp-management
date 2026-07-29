using MediatR;
using System;

namespace EMS.Application.Features.Payroll.Commands
{
    /// <summary>Un-deletes a soft-deleted salary structure.</summary>
    public class RestoreSalaryStructureCommand : IRequest
    {
        public Guid Id { get; set; }
    }
}
