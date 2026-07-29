using MediatR;
using System;

namespace EMS.Application.Features.Assets.Commands
{
    public class UpdateAssetCommand : IRequest
    {
        public Guid Id { get; set; }
        public string Category { get; set; } = null!;
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public string? SerialNumber { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public decimal? PurchaseCost { get; set; }
        public string? Notes { get; set; }
    }
}
