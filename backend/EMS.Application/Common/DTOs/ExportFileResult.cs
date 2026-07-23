namespace EMS.Application.Common.DTOs
{
    /// <summary>File payload returned by the Export APIs (Excel or PDF).</summary>
    public class ExportFileResult
    {
        public byte[] Content { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public string FileName { get; set; } = null!;
    }
}
