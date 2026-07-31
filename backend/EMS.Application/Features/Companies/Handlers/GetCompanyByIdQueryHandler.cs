using EMS.Application.Features.Companies.DTOs;
using EMS.Application.Features.Companies.Queries;
using EMS.Application.Interfaces;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Companies.Handlers
{
    public class GetCompanyByIdQueryHandler : IRequestHandler<GetCompanyByIdQuery, CompanyDetailDto?>
    {
        private readonly ICompanyRepository _repo;

        public GetCompanyByIdQueryHandler(ICompanyRepository repo) => _repo = repo;

        public async Task<CompanyDetailDto?> Handle(GetCompanyByIdQuery request, CancellationToken cancellationToken)
        {
            var company = await _repo.GetByIdAsync(request.Id, cancellationToken);
            if (company == null) return null;

            var employeeCount = await _repo.GetEmployeeCountAsync(company.Id, cancellationToken);
            var admins = await _repo.GetAdminsAsync(company.Id, cancellationToken);

            var dto = CompanyDto.FromEntity(company);
            return new CompanyDetailDto
            {
                Id = dto.Id,
                Name = dto.Name,
                Status = dto.Status,
                Timezone = dto.Timezone,
                Currency = dto.Currency,
                LogoUrl = dto.LogoUrl,
                RegisteredAtUtc = dto.RegisteredAtUtc,
                ApprovedAtUtc = dto.ApprovedAtUtc,
                SuspendedAtUtc = dto.SuspendedAtUtc,
                SuspendedReason = dto.SuspendedReason,
                RejectedAtUtc = dto.RejectedAtUtc,
                RejectedReason = dto.RejectedReason,
                IsDeleted = dto.IsDeleted,
                CreatedAtUtc = dto.CreatedAtUtc,
                UpdatedAtUtc = dto.UpdatedAtUtc,
                EmployeeCount = employeeCount,
                Admins = admins.Select(a => new CompanyAdminDto
                {
                    UserId = a.Id,
                    UserName = a.UserName,
                    Email = a.Email,
                    IsActive = a.IsActive
                }).ToList()
            };
        }
    }
}
