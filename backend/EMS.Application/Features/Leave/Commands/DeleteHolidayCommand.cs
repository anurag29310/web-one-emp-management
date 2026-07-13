using MediatR;
using System;

namespace EMS.Application.Features.Leave.Commands
{
    public class DeleteHolidayCommand : IRequest
    {
        public Guid Id { get; set; }
    }
}
