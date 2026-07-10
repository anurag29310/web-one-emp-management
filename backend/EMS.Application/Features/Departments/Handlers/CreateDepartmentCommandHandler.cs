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
        private readonly ILogger<CreateDepartmentCommandHandler> _logger;

        public CreateDepartmentCommandHandler(IDepartmentRepository repo, ILogger<CreateDepartmentCommandHandler> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<Department> Handle(CreateDepartmentCommand request, CancellationToken cancellationToken)
        {
            if (await _repo.NameExistsAsync(request.Name, ct: cancellationToken))
                throw new InvalidOperationException("Department name already exists.");
            if (!string.IsNullOrWhiteSpace(request.Code) && await _repo.CodeExistsAsync(request.Code, ct: cancellationToken))
                throw new InvalidOperationException("Department code already exists.");

            var dept = new Department
            {
                Id = Guid.NewGuid(),
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
