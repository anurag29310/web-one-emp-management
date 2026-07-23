using System;

namespace EMS.Application.DTOs
{
    public class HealthStatusDto
    {
        public string Status { get; set; } = "Healthy";
        public DateTime TimestampUtc { get; set; }
    }

    public class ReadinessStatusDto
    {
        public string Status { get; set; } = "Healthy";
        public bool DatabaseConnected { get; set; }
        public DateTime TimestampUtc { get; set; }
    }
}
