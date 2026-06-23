using EMS.Application.Features.Documents.Commands;
using EMS.Application.Features.Documents.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EMS.API.Controllers
{
    [ApiController]
    [Route("api/employees/{employeeId}/documents")]
    public class EmployeeDocumentsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public EmployeeDocumentsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Upload(Guid employeeId, IFormFile file, [FromForm] string documentType, [FromForm] DateTime? expiresAtUtc)
        {
            if (file == null) return BadRequest("file is required");

            // basic authorization: employees can upload their own documents, or roles Admin/HR
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var isAdminOrHr = User.IsInRole("Admin") || User.IsInRole("HR");
            if (!isAdminOrHr && userIdClaim != null && Guid.Parse(userIdClaim) != employeeId)
                return Forbid();

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var bytes = ms.ToArray();

            var cmd = new UploadDocumentCommand
            {
                EmployeeId = employeeId,
                DocumentType = documentType,
                FileName = file.FileName,
                ContentType = file.ContentType ?? "application/octet-stream",
                Content = bytes,
                ExpiresAtUtc = expiresAtUtc,
                UploadedBy = userIdClaim != null ? Guid.Parse(userIdClaim) : (Guid?)null
            };

            var id = await _mediator.Send(cmd);
            return CreatedAtAction(nameof(Download), new { employeeId = employeeId, documentId = id }, id);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> List(Guid employeeId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null, [FromQuery] string? documentType = null)
        {
            var q = new GetDocumentsQuery { EmployeeId = employeeId, Page = page, PageSize = pageSize, Search = search, DocumentType = documentType };
            var result = await _mediator.Send(q);
            return Ok(result);
        }

        [HttpGet("{documentId}/download")]
        [Authorize]
        public async Task<IActionResult> Download(Guid employeeId, Guid documentId)
        {
            var result = await _mediator.Send(new DownloadDocumentQuery { DocumentId = documentId });
            if (result == null) return NotFound();
            return File(result.Content, result.ContentType, result.FileName);
        }

        [HttpDelete("{documentId}")]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Delete(Guid employeeId, Guid documentId)
        {
            await _mediator.Send(new DeleteDocumentCommand { DocumentId = documentId });
            return NoContent();
        }
    }
}
