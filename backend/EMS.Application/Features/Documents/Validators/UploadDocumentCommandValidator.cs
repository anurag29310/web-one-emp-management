using EMS.Application.Features.Documents.Commands;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EMS.Application.Features.Documents.Validators
{
    public class UploadDocumentCommandValidator : AbstractValidator<UploadDocumentCommand>
    {
        public const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

        private static readonly Dictionary<string, string[]> AllowedExtensionsByContentType = new()
        {
            ["application/pdf"] = new[] { ".pdf" },
            ["image/jpeg"] = new[] { ".jpg", ".jpeg" },
            ["image/png"] = new[] { ".png" },
            ["application/msword"] = new[] { ".doc" },
            ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"] = new[] { ".docx" }
        };

        public UploadDocumentCommandValidator()
        {
            RuleFor(x => x.EmployeeId).NotEmpty();

            RuleFor(x => x.DocumentType).NotEmpty().MaximumLength(100);

            RuleFor(x => x.FileName)
                .NotEmpty().MaximumLength(255)
                .Must(name => name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0)
                    .WithMessage("FileName contains invalid characters.")
                .Must(name => !name.Contains("..") && !name.Contains('/') && !name.Contains('\\'))
                    .WithMessage("FileName must not contain path separators.");

            RuleFor(x => x.ContentType)
                .Must(ct => AllowedExtensionsByContentType.ContainsKey(ct))
                .WithMessage($"Unsupported content type. Allowed types: {string.Join(", ", AllowedExtensionsByContentType.Keys)}.");

            RuleFor(x => x)
                .Must(cmd => AllowedExtensionsByContentType.TryGetValue(cmd.ContentType, out var exts)
                    && exts.Contains(Path.GetExtension(cmd.FileName), StringComparer.OrdinalIgnoreCase))
                .WithMessage("File extension does not match the declared content type.")
                .When(cmd => !string.IsNullOrEmpty(cmd.FileName) && AllowedExtensionsByContentType.ContainsKey(cmd.ContentType));

            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("File content is empty.")
                .Must(c => c == null || c.Length <= MaxFileSizeBytes)
                    .WithMessage($"File exceeds the maximum allowed size of {MaxFileSizeBytes / (1024 * 1024)} MB.");

            RuleFor(x => x)
                .Must(cmd => MatchesSignature(cmd.ContentType, cmd.Content))
                .WithMessage("File content does not match its declared content type.")
                .When(cmd => cmd.Content != null && cmd.Content.Length > 0 && AllowedExtensionsByContentType.ContainsKey(cmd.ContentType));
        }

        // Verifies the actual file bytes against known magic numbers so a spoofed
        // Content-Type header (fully attacker-controlled) can't smuggle in a disallowed file type.
        private static bool MatchesSignature(string contentType, byte[] content)
        {
            return contentType switch
            {
                "application/pdf" => StartsWith(content, 0x25, 0x50, 0x44, 0x46, 0x2D), // %PDF-
                "image/jpeg" => StartsWith(content, 0xFF, 0xD8, 0xFF),
                "image/png" => StartsWith(content, 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A),
                "application/msword" => StartsWith(content, 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1),
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => StartsWith(content, 0x50, 0x4B, 0x03, 0x04),
                _ => false
            };
        }

        private static bool StartsWith(byte[] content, params byte[] signature)
        {
            if (content.Length < signature.Length) return false;
            for (var i = 0; i < signature.Length; i++)
                if (content[i] != signature[i]) return false;
            return true;
        }
    }
}
