using ClosedXML.Excel;
using EMS.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace EMS.Infrastructure.Export
{
    public class ClosedXmlExportService : IExcelExportService
    {
        public Task<byte[]> GenerateAsync(string sheetName, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<object?>> rows)
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add(sheetName);

            for (var col = 0; col < headers.Count; col++)
            {
                var cell = sheet.Cell(1, col + 1);
                cell.Value = headers[col];
                cell.Style.Font.Bold = true;
            }

            var rowIndex = 2;
            foreach (var row in rows)
            {
                for (var col = 0; col < row.Count; col++)
                {
                    SetCellValue(sheet.Cell(rowIndex, col + 1), row[col]);
                }
                rowIndex++;
            }

            sheet.Columns(1, headers.Count).AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return Task.FromResult(stream.ToArray());
        }

        private static void SetCellValue(IXLCell cell, object? value)
        {
            switch (value)
            {
                case null:
                    cell.SetValue(string.Empty);
                    break;
                case DateTime dt:
                    cell.SetValue(dt);
                    cell.Style.DateFormat.Format = "yyyy-mm-dd hh:mm";
                    break;
                case bool b:
                    cell.SetValue(b);
                    break;
                case int or long or decimal or double or float:
                    cell.SetValue(Convert.ToDouble(value));
                    break;
                default:
                    cell.SetValue(value.ToString() ?? string.Empty);
                    break;
            }
        }
    }
}
