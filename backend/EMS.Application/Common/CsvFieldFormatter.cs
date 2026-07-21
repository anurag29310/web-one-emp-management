using System.Globalization;

namespace EMS.Application.Common
{
    /// <summary>RFC 4180-style field escaping shared by CSV report exports.</summary>
    public static class CsvFieldFormatter
    {
        private static readonly char[] FormulaTriggers = { '=', '+', '-', '@', '\t', '\r' };

        public static string Escape(object? value)
        {
            var text = value switch
            {
                null => string.Empty,
                System.DateTime dt => dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                _ => value.ToString() ?? string.Empty
            };

            // Neutralize CSV/formula injection (CWE-1236): a leading =, +, -, @, tab, or CR is
            // interpreted as a formula by Excel/Sheets when the export is opened. Prefixing with
            // an apostrophe forces text interpretation in both, without changing the visible value.
            if (text.Length > 0 && System.Array.IndexOf(FormulaTriggers, text[0]) >= 0)
                text = "'" + text;

            if (text.Contains(',') || text.Contains('"') || text.Contains('\n') || text.Contains('\r'))
                return "\"" + text.Replace("\"", "\"\"") + "\"";

            return text;
        }
    }
}
