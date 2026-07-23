using PdfSharp.Fonts;
using System;
using System.IO;

namespace EMS.Infrastructure.Pdf
{
    // PDFsharp 6.x has no GDI+ dependency, and therefore no built-in font access on any platform
    // (Windows included) — a resolver must supply font bytes itself. Fonts are embedded resources
    // (see EMS.Infrastructure.csproj) so this works identically in local dev and in the Linux
    // Docker image, with no host font packages required.
    public sealed class EmbeddedFontResolver : IFontResolver
    {
        public const string FamilyName = "PT Sans";

        private static readonly Lazy<byte[]> RegularBytes = new(() => ReadEmbeddedFont("PTSans-Regular.ttf"));
        private static readonly Lazy<byte[]> BoldBytes = new(() => ReadEmbeddedFont("PTSans-Bold.ttf"));

        public string DefaultFontName => FamilyName;

        public byte[] GetFont(string faceName) => faceName switch
        {
            "PTSans#Regular" => RegularBytes.Value,
            "PTSans#Bold" => BoldBytes.Value,
            _ => throw new ArgumentException($"Unknown font face '{faceName}'.", nameof(faceName))
        };

        public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            if (!familyName.Equals(FamilyName, StringComparison.OrdinalIgnoreCase))
                return null;

            return new FontResolverInfo(isBold ? "PTSans#Bold" : "PTSans#Regular");
        }

        private static byte[] ReadEmbeddedFont(string fileName)
        {
            var assembly = typeof(EmbeddedFontResolver).Assembly;
            var resourceName = $"EMS.Infrastructure.Pdf.Fonts.{fileName}";
            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException(
                    $"Embedded font resource '{resourceName}' not found. Available resources: " +
                    string.Join(", ", assembly.GetManifestResourceNames()));
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            return memory.ToArray();
        }
    }
}
