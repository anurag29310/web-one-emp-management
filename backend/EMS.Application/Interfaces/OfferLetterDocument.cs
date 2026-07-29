using System;

namespace EMS.Application.Interfaces
{
    // Rendering input for IPdfService.GenerateOfferLetterPdfAsync — deliberately separate from the
    // persisted Offer entity, matching PayslipDocument's precedent.
    public class OfferLetterDocument
    {
        public string OfferNumber { get; set; } = null!;
        public string CandidateName { get; set; } = null!;
        public string DesignationName { get; set; } = null!;
        public string? DepartmentName { get; set; }
        public decimal OfferedSalary { get; set; }
        public DateTime JoiningDate { get; set; }
        public DateTime IssuedAtUtc { get; set; }
        public string? Notes { get; set; }
    }
}
