using EMS.Application.Interfaces;
using EMS.Infrastructure.Pdf;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace EMS.Tests
{
    public class PdfSharpDocumentServiceTests
    {
        private static PayslipDocument CreateDocument(
            IReadOnlyList<PayslipLineItem>? allowances = null,
            IReadOnlyList<PayslipLineItem>? deductions = null)
        {
            return new PayslipDocument
            {
                EmployeeName = "Jane Doe",
                EmployeeCode = "EMP-042",
                Designation = "Senior Engineer",
                Department = "Engineering",
                PeriodStart = new DateTime(2026, 6, 1),
                PeriodEnd = new DateTime(2026, 6, 30),
                GeneratedAtUtc = new DateTime(2026, 7, 1, 9, 0, 0, DateTimeKind.Utc),
                Basic = 5000m,
                Allowances = allowances ?? new List<PayslipLineItem> { new() { Name = "House Rent", Amount = 500m } },
                Deductions = deductions ?? new List<PayslipLineItem> { new() { Name = "Tax", Amount = 300m } },
                GrossPay = 5500m,
                NetPay = 5200m
            };
        }

        private static void AssertValidPdf(byte[] bytes)
        {
            Assert.True(bytes.Length > 0);
            Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
        }

        [Fact]
        public async Task GeneratePayslipPdfAsync_ProducesBytesStartingWithPdfMagicNumber()
        {
            var service = new PdfSharpDocumentService();

            var bytes = await service.GeneratePayslipPdfAsync(CreateDocument());

            AssertValidPdf(bytes);
        }

        [Fact]
        public async Task GeneratePayslipPdfAsync_WithNoAllowancesOrDeductions_StillProducesValidPdf()
        {
            var service = new PdfSharpDocumentService();

            var document = CreateDocument(
                allowances: Array.Empty<PayslipLineItem>(),
                deductions: Array.Empty<PayslipLineItem>());

            var bytes = await service.GeneratePayslipPdfAsync(document);

            AssertValidPdf(bytes);
        }

        [Fact]
        public async Task GeneratePayslipPdfAsync_WithMissingOptionalFields_DoesNotThrow()
        {
            var service = new PdfSharpDocumentService();
            var document = CreateDocument();
            document.Designation = null;
            document.Department = null;

            var bytes = await service.GeneratePayslipPdfAsync(document);

            AssertValidPdf(bytes);
        }

        [Fact]
        public async Task GeneratePayslipPdfAsync_CalledConcurrently_DoesNotThrow()
        {
            // GlobalFontSettings.FontResolver is process-global and assigned once from a static
            // constructor; this guards against races if multiple requests trigger the service's
            // first use at the same time.
            var service = new PdfSharpDocumentService();
            var document = CreateDocument();

            var tasks = new List<Task<byte[]>>();
            for (var i = 0; i < 8; i++)
                tasks.Add(service.GeneratePayslipPdfAsync(document));

            var results = await Task.WhenAll(tasks);

            Assert.All(results, AssertValidPdf);
        }
    }
}
