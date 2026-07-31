using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Departments.Handlers
{
    public class UpdateDepartmentCommandHandler : IRequestHandler<UpdateDepartmentCommand, Department>
    {
        private readonly IDepartmentRepository _repo;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<UpdateDepartmentCommandHandler> _logger;

        public UpdateDepartmentCommandHandler(IDepartmentRepository repo, ICurrentUserService currentUser, ILogger<UpdateDepartmentCommandHandler> logger)
        {
            _repo = repo;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<Department> Handle(UpdateDepartmentCommand request, CancellationToken cancellationToken)
        {
            var companyId = _currentUser.CompanyId!.Value;
            var dept = await _repo.GetByIdAsync(request.Id, companyId, cancellationToken)
                ?? throw new InvalidOperationException($"Department {request.Id} not found.");

            if (await _repo.NameExistsAsync(request.Name, companyId, request.Id, cancellationToken))
                throw new InvalidOperationException("Department name already exists.");
            if (!string.IsNullOrWhiteSpace(request.Code) && await _repo.CodeExistsAsync(request.Code, companyId, request.Id, cancellationToken))
                throw new InvalidOperationException("Department code already exists.");

            dept.Name = request.Name;
            dept.Code = request.Code;
            dept.Description = request.Description;
            dept.HeadEmployeeId = request.HeadEmployeeId;
            dept.UpdatedAtUtc = DateTime.UtcNow;

            await _repo.UpdateAsync(dept, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Updated department {DepartmentId}", dept.Id);
            return dept;
        }
    }
}
