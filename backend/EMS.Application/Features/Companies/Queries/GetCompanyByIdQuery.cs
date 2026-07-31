using EMS.Application.Features.Companies.DTOs;
using MediatR;
using System;

namespace EMS.Application.Features.Companies.Queries
{
    public class GetCompanyByIdQuery : IRequest<CompanyDetailDto?>
    {
        public Guid Id { get; set; }
    }
}
