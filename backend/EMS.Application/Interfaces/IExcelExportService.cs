using System.Collections.Generic;
using System.Threading.Tasks;

namespace EMS.Application.Interfaces
{
    /// <summary>Generates a single-sheet Excel (.xlsx) workbook from tabular data.</summary>
    public interface IExcelExportService
    {
        Task<byte[]> GenerateAsync(string sheetName, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<object?>> rows);
    }
}
