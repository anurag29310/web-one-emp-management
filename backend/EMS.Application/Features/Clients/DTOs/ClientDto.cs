using EMS.Domain.Entities;
using System;

namespace EMS.Application.Features.Clients.DTOs
{
    public class ClientDto
    {
        public Guid Id { get; set; }
        public string ClientName { get; set; } = null!;
        public string CompanyName { get; set; } = null!;
        public string ContactPerson { get; set; } = null!;
        public string MobileNumber { get; set; } = null!;
        public string? AlternateMobile { get; set; }
        public string Email { get; set; } = null!;
        public string? GstNumber { get; set; }
        public string AddressLine1 { get; set; } = null!;
        public string? AddressLine2 { get; set; }
        public string City { get; set; } = null!;
        public string? State { get; set; }
        public string Country { get; set; } = null!;
        public string PostalCode { get; set; } = null!;
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; }
        public bool IsArchived { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }

        public static ClientDto FromEntity(Client c) => new()
        {
            Id = c.Id,
            ClientName = c.ClientName,
            CompanyName = c.CompanyName,
            ContactPerson = c.ContactPerson,
            MobileNumber = c.MobileNumber,
            AlternateMobile = c.AlternateMobile,
            Email = c.Email,
            GstNumber = c.GstNumber,
            AddressLine1 = c.AddressLine1,
            AddressLine2 = c.AddressLine2,
            City = c.City,
            State = c.State,
            Country = c.Country,
            PostalCode = c.PostalCode,
            Latitude = c.Latitude,
            Longitude = c.Longitude,
            Notes = c.Notes,
            IsActive = c.IsActive,
            IsArchived = c.IsArchived,
            IsDeleted = c.IsDeleted,
            CreatedAtUtc = c.CreatedAtUtc,
            UpdatedAtUtc = c.UpdatedAtUtc
        };
    }
}
