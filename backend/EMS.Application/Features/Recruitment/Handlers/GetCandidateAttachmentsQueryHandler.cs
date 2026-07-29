using EMS.Application.Features.Recruitment.DTOs;
using EMS.Application.Features.Recruitment.Queries;
using EMS.Application.Interfaces;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Recruitment.Handlers
{
    public class GetCandidateAttachmentsQueryHandler : IRequestHandler<GetCandidateAttachmentsQuery, IEnumerable<CandidateAttachmentDto>>
    {
        private readonly IRecruitmentRepository _repo;

        public GetCandidateAttachmentsQueryHandler(IRecruitmentRepository repo) => _repo = repo;

        public async Task<IEnumerable<CandidateAttachmentDto>> Handle(GetCandidateAttachmentsQuery request, CancellationToken cancellationToken)
        {
            var attachments = await _repo.GetAttachmentsAsync(request.CandidateId, cancellationToken);
            return attachments.Select(CandidateAttachmentDto.FromEntity);
        }
    }
}
