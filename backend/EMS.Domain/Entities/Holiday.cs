using System;

namespace EMS.Domain.Entities
{
    public class Holiday
    {
        public Guid Id { get; set; }
        public Guid? OfficeLocationId { get; set; }
        public string Name { get; set; } = null!;
        public DateTime HolidayDate { get; set; }
        public bool IsOptional { get; set; }
    }
}
