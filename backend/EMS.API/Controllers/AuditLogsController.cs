using EMS.Application.Common.DTOs;
using EMS.Application.Features.AuditLogs.DTOs;
using EMS.Application.Features.AuditLogs.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.API.Controllers
{
    [ApiController]
    [Route("api/v1/audit-logs")]
    [Authorize]
    [Produces("application/json")]
    public class AuditLogsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AuditLogsController(IMediator mediator) => _mediator = mediator;

        /// <summary>List audit logs with filtering and pagination.</summary>
        [HttpGet]
        [Authorize(Policy = "CanViewAuditLogs")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<AuditLogDto>>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] GetAuditLogsQuery query, CancellationToken ct)
        {
            var result = await _mediator.Send(query, ct);
            return Ok(ApiResponse<PagedResult<AuditLogDto>>.Success(result));
        }

        /// <summary>Get a single audit log entry by ID.</summary>
        [HttpGet("{id:guid}")]
        [Authorize(Policy = "CanViewAuditLogs")]
        [ProducesResponseType(typeof(ApiResponse<AuditLogDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            var result = await _mediator.Send(new GetAuditLogByIdQuery { Id = id }, ct);
            if (result == null) return NotFound();
            return Ok(ApiResponse<AuditLogDto>.Success(AuditLogDto.FromEntity(result)));
        }

        /// <summary>Get the audit history for a specific entity.</summary>
        [HttpGet("entity/{entityName}/{entityId:guid}")]
        [Authorize(Roles = "Admin,HR")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<AuditLogDto>>), 200)]
        public async Task<IActionResult> GetForEntity(
            string entityName,
            Guid entityId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            var result = await _mediator.Send(
                new GetAuditLogsForEntityQuery { EntityName = entityName, EntityId = entityId, Page = page, PageSize = pageSize }, ct);
            return Ok(ApiResponse<PagedResult<AuditLogDto>>.Success(result));
        }
    }
}
