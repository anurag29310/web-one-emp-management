using MediatR;
using System;

namespace EMS.Application.Features.Performance.Commands
{
    public class DeletePromotionCommand : IRequest
    {
        public Guid Id { get; set; }
        public Guid RequestingUserId { get; set; }
    }
}
