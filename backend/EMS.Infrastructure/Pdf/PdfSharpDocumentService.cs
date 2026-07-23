using EMS.Application.DTOs;
using EMS.Application.Interfaces;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EMS.Infrastructure.Pdf
{
    public class PdfSharpDocumentService : IPdfService
    {
        private const double PageMargin = 36; // points, ~0.5in
        private const double ContentWidth = 595 - 2 * PageMargin; // A4 width in points minus margins

        static PdfSharpDocumentService()
        {
            // GlobalFontSettings.FontResolver can only be assigned once per process; a second
            // assignment throws, which would otherwise break tests that construct this service
            // more than once.
            if (GlobalFontSettings.FontResolver is null)
                GlobalFontSettings.FontResolver = new EmbeddedFontResolver();
        }

        public Task<byte[]> GeneratePayslipPdfAsync(PayslipDocument document)
        {
            using var pdf = new PdfDocument();
            var page = pdf.AddPage();
            page.Size = PdfSharp.PageSize.A4;
            using var gfx = XGraphics.FromPdfPage(page);
            var w = new PdfPageWriter(gfx, PageMargin, ContentWidth);

            w.Title("Employee Management System", "Payslip");

            w.Line(document.EmployeeName, w.FontBold);
            w.Line($"Employee Code: {document.EmployeeCode}");
            if (!string.IsNullOrWhiteSpace(document.Designation))
                w.Line($"Designation: {document.Designation}");
            if (!string.IsNullOrWhiteSpace(document.Department))
                w.Line($"Department: {document.Department}");
            w.Line($"Pay Period: {document.PeriodStart:yyyy-MM-dd} to {document.PeriodEnd:yyyy-MM-dd}");
            w.Line($"Generated: {document.GeneratedAtUtc:yyyy-MM-dd HH:mm} UTC");

            w.Spacer(6);
            w.Rule();
            w.Spacer(6);

            var earnings = new List<(string, string)> { ("Basic", Format(document.Basic)) };
            earnings.AddRange(document.Allowances.Select(a => (a.Name, Format(a.Amount))));
            w.Table("Earnings", earnings);

            w.Spacer(6);
            w.Table("Deductions", document.Deductions.Count == 0
                ? new List<(string, string)> { ("None", Format(0m)) }
                : document.Deductions.Select(d => (d.Name, Format(d.Amount))).ToList());

            w.Spacer(6);
            w.Rule();
            w.Spacer(6);

            var totalDeductions = document.Deductions.Sum(d => d.Amount);
            w.LineRight($"Gross Pay: {Format(document.GrossPay)}");
            w.LineRight($"Total Deductions: {Format(totalDeductions)}");
            w.LineRight($"Net Pay: {Format(document.NetPay)}", w.FontBoldLarge);

            w.Footer("This is a system-generated payslip and does not require a signature.");

            return Task.FromResult(Render(pdf));
        }

        public Task<byte[]> GenerateDashboardSummaryPdfAsync(DashboardSummaryDto summary, DateTime asOfDate, Guid? departmentId)
        {
            using var pdf = new PdfDocument();
            var page = pdf.AddPage();
            page.Size = PdfSharp.PageSize.A4;
            using var gfx = XGraphics.FromPdfPage(page);
            var w = new PdfPageWriter(gfx, PageMargin, ContentWidth);

            w.Title("Employee Management System", "Dashboard Summary");
            w.Line($"As of {asOfDate:yyyy-MM-dd}");

            w.Spacer(6);
            w.Rule();
            w.Spacer(6);

            w.Table("Employees", new List<(string, string)>
            {
                ("Total", summary.TotalEmployees.ToString(CultureInfo.InvariantCulture)),
                ("Active", summary.ActiveEmployees.ToString(CultureInfo.InvariantCulture)),
                ("Inactive", summary.InactiveEmployees.ToString(CultureInfo.InvariantCulture))
            });

            w.Spacer(6);
            w.Table("Attendance", new List<(string, string)>
            {
                ("Present", summary.Attendance.Present.ToString(CultureInfo.InvariantCulture)),
                ("Absent", summary.Attendance.Absent.ToString(CultureInfo.InvariantCulture)),
                ("Late", summary.Attendance.Late.ToString(CultureInfo.InvariantCulture)),
                ("On Leave", summary.Attendance.OnLeave.ToString(CultureInfo.InvariantCulture))
            });

            w.Spacer(6);
            w.Table("Leave", new List<(string, string)>
            {
                ("Pending", summary.Leave.Pending.ToString(CultureInfo.InvariantCulture)),
                ("Approved Today", summary.Leave.ApprovedToday.ToString(CultureInfo.InvariantCulture)),
                ("Rejected Today", summary.Leave.RejectedToday.ToString(CultureInfo.InvariantCulture))
            });

            w.Spacer(6);
            w.Rule();
            w.Spacer(6);

            var departments = summary.Departments.ToList();
            w.Table("Departments — Active Employees", departments.Count == 0
                ? new List<(string, string)> { ("None", "0") }
                : departments.Select(d => (d.DepartmentName, d.ActiveEmployees.ToString(CultureInfo.InvariantCulture))).ToList());

            w.Footer("This is a system-generated report.");

            return Task.FromResult(Render(pdf));
        }

        private static byte[] Render(PdfDocument pdf)
        {
            using var stream = new MemoryStream();
            pdf.Save(stream, closeStream: false);
            return stream.ToArray();
        }

        private static string Format(decimal amount) => amount.ToString("N2", CultureInfo.InvariantCulture);

        // Minimal top-to-bottom flow writer around XGraphics — sized for the two short, fixed-shape
        // documents this service produces. Not a general-purpose layout engine: it assumes content
        // fits on a single page, which holds comfortably for a payslip or a dashboard summary.
        private sealed class PdfPageWriter
        {
            private readonly XGraphics _gfx;
            private readonly double _left;
            private readonly double _right;
            private double _y;

            public XFont FontRegular { get; } = new(EmbeddedFontResolver.FamilyName, 10, XFontStyleEx.Regular);
            public XFont FontBold { get; } = new(EmbeddedFontResolver.FamilyName, 10, XFontStyleEx.Bold);
            public XFont FontBoldLarge { get; } = new(EmbeddedFontResolver.FamilyName, 13, XFontStyleEx.Bold);
            public XFont FontHeading { get; } = new(EmbeddedFontResolver.FamilyName, 16, XFontStyleEx.Bold);
            public XFont FontSubheading { get; } = new(EmbeddedFontResolver.FamilyName, 12, XFontStyleEx.Bold);
            public XFont FontSmall { get; } = new(EmbeddedFontResolver.FamilyName, 8, XFontStyleEx.Regular);

            public PdfPageWriter(XGraphics gfx, double margin, double contentWidth)
            {
                _gfx = gfx;
                _left = margin;
                _right = margin + contentWidth;
                _y = margin;
            }

            public void Title(string heading, string subheading)
            {
                _gfx.DrawString(heading, FontHeading, XBrushes.Black, new XPoint(_left, _y + FontHeading.Height));
                _y += FontHeading.Height + 2;
                _gfx.DrawString(subheading, FontSubheading, XBrushes.DimGray, new XPoint(_left, _y + FontSubheading.Height));
                _y += FontSubheading.Height + 6;
                Rule();
                Spacer(6);
            }

            public void Line(string text) => Line(text, FontRegular);

            public void Line(string text, XFont font)
            {
                _gfx.DrawString(text, font, XBrushes.Black, new XPoint(_left, _y + font.Height));
                _y += font.Height + 2;
            }

            public void LineRight(string text) => LineRight(text, FontRegular);

            public void LineRight(string text, XFont font)
            {
                var rect = new XRect(_left, _y, _right - _left, font.Height + 4);
                _gfx.DrawString(text, font, XBrushes.Black, rect, XStringFormats.TopRight);
                _y += font.Height + 4;
            }

            public void Rule()
            {
                _gfx.DrawLine(new XPen(XColor.FromArgb(200, 200, 200)), _left, _y, _right, _y);
            }

            public void Spacer(double height) => _y += height;

            public void Table(string title, IReadOnlyList<(string Label, string Value)> rows)
            {
                Line(title, FontBold);
                foreach (var (label, value) in rows)
                {
                    var rect = new XRect(_left, _y, _right - _left, FontRegular.Height + 3);
                    _gfx.DrawString(label, FontRegular, XBrushes.Black, rect, XStringFormats.TopLeft);
                    _gfx.DrawString(value, FontRegular, XBrushes.Black, rect, XStringFormats.TopRight);
                    _y += FontRegular.Height + 3;
                }
            }

            public void Footer(string text)
            {
                var rect = new XRect(_left, 800, _right - _left, FontSmall.Height + 4);
                _gfx.DrawString(text, FontSmall, XBrushes.Gray, rect, XStringFormats.TopCenter);
            }
        }
    }
}
