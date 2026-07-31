using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Departments.Handlers
{
    public class CreateDepartmentCommandHandler : IRequestHandler<CreateDepartmentCommand, Department>
    {
        private readonly IDepartmentRepository _repo;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<CreateDepartmentCommandHandler> _logger;

        public CreateDepartmentCommandHandler(IDepartmentRepository repo, ICurrentUserService currentUser, ILogger<CreateDepartmentCommandHandler> logger)
        {
            _repo = repo;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<Department> Handle(CreateDepartmentCommand request, CancellationToken cancellationToken)
        {
            var companyId = _currentUser.CompanyId!.Value;
            if (await _repo.NameExistsAsync(request.Name, companyId, ct: cancellationToken))
                throw new InvalidOperationException("Department name already exists.");
            if (!string.IsNullOrWhiteSpace(request.Code) && await _repo.CodeExistsAsync(request.Code, companyId, ct: cancellationToken))
                throw new InvalidOperationException("Department code already exists.");

            var dept = new Department
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Name = request.Name,
                Code = request.Code,
                Description = request.Description,
                HeadEmployeeId = request.HeadEmployeeId,
                CreatedAtUtc = DateTime.UtcNow,
                IsDeleted = false
            };

            await _repo.AddAsync(dept, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Created department {DepartmentName} ({DepartmentId})", dept.Name, dept.Id);
            return dept;
        }
    }
}
