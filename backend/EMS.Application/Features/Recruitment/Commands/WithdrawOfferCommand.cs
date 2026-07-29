using MediatR;
using System;

namespace EMS.Application.Features.Recruitment.Commands
{
    /// <summary>The company pulls back an offer, as opposed to Reject which is the candidate's decision.</summary>
    public class WithdrawOfferCommand : IRequest
    {
        public Guid Id { get; set; }
    }
}
