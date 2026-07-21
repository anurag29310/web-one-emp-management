using EMS.Domain.Entities;
using MediatR;
using System;

namespace EMS.Application.Features.AuditLogs.Queries
{
    public class GetAuditLogByIdQuery : IRequest<AuditLog?>
    {
        public Guid Id { get; set; }
    }
}
