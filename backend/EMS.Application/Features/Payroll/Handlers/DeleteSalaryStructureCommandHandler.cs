using EMS.Application.Features.Payroll.Commands;
using EMS.Application.Interfaces;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Payroll.Handlers
{
    public class DeleteSalaryStructureCommandHandler : IRequestHandler<DeleteSalaryStructureCommand>
    {
        private readonly IPayrollRepository _repo;

        public DeleteSalaryStructureCommandHandler(IPayrollRepository repo)
        {
            _repo = repo;
        }

        public async Task<Unit> Handle(DeleteSalaryStructureCommand request, CancellationToken cancellationToken)
        {
            var deleted = await _repo.DeleteSalaryStructureAsync(request.Id);
            if (!deleted) throw new InvalidOperationException("Salary structure not found.");

            await _repo.SaveChangesAsync();
            return Unit.Value;
        }

        Task IRequestHandler<DeleteSalaryStructureCommand>.Handle(DeleteSalaryStructureCommand request, CancellationToken cancellationToken)
        {
            return Handle(request, cancellationToken);
        }
    }
}
