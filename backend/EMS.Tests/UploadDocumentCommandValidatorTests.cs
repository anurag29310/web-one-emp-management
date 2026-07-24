using EMS.Application.Features.Documents.Commands;
using EMS.Application.Features.Documents.Validators;
using System;
using System.Linq;
using Xunit;

namespace EMS.Tests
{
    public class UploadDocumentCommandValidatorTests
    {
        private static readonly byte[] PdfBytes = { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 }; // %PDF-1.4
        private static readonly byte[] PngBytes = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00 };
        private static readonly byte[] ExeBytes = { 0x4D, 0x5A, 0x90, 0x00 }; // "MZ" - Windows PE header

        private static UploadDocumentCommand ValidCommand() => new()
        {
            EmployeeId = Guid.NewGuid(),
            DocumentType = "IDProof",
            FileName = "id.pdf",
            ContentType = "application/pdf",
            Content = PdfBytes,
            UploadedBy = Guid.NewGuid()
        };

        [Fact]
        public void Validate_ValidPdf_Passes()
        {
            var result = new UploadDocumentCommandValidator().Validate(ValidCommand());
            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_ValidPng_Passes()
        {
            var cmd = ValidCommand();
            cmd.FileName = "photo.png";
            cmd.ContentType = "image/png";
            cmd.Content = PngBytes;

            var result = new UploadDocumentCommandValidator().Validate(cmd);
            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_EmptyContent_Fails()
        {
            var cmd = ValidCommand();
            cmd.Content = Array.Empty<byte>();

            var result = new UploadDocumentCommandValidator().Validate(cmd);
            Assert.False(result.IsValid);
        }

        [Fact]
        public void Validate_ContentExceedsMaxSize_Fails()
        {
            var cmd = ValidCommand();
            cmd.Content = new byte[UploadDocumentCommandValidator.MaxFileSizeBytes + 1];
            PdfBytes.CopyTo(cmd.Content, 0);

            var result = new UploadDocumentCommandValidator().Validate(cmd);
            Assert.False(result.IsValid);
        }

        [Fact]
        public void Validate_DisallowedContentType_Fails()
        {
            var cmd = ValidCommand();
            cmd.ContentType = "application/x-msdownload";
            cmd.FileName = "malware.exe";
            cmd.Content = ExeBytes;

            var result = new UploadDocumentCommandValidator().Validate(cmd);
            Assert.False(result.IsValid);
        }

        [Fact]
        public void Validate_SpoofedContentTypeWithMismatchedBytes_Fails()
        {
            // Attacker declares an allowed content-type but the actual bytes are an executable.
            var cmd = ValidCommand();
            cmd.ContentType = "application/pdf";
            cmd.FileName = "malware.pdf";
            cmd.Content = ExeBytes;

            var result = new UploadDocumentCommandValidator().Validate(cmd);
            Assert.False(result.IsValid);
        }

        [Fact]
        public void Validate_ExtensionDoesNotMatchContentType_Fails()
        {
            var cmd = ValidCommand();
            cmd.ContentType = "application/pdf";
            cmd.FileName = "id.png";

            var result = new UploadDocumentCommandValidator().Validate(cmd);
            Assert.False(result.IsValid);
        }

        [Theory]
        [InlineData("../../etc/passwd")]
        [InlineData("..\\..\\windows\\win.ini")]
        [InlineData("folder/file.pdf")]
        public void Validate_FileNameWithPathTraversal_Fails(string fileName)
        {
            var cmd = ValidCommand();
            cmd.FileName = fileName;

            var result = new UploadDocumentCommandValidator().Validate(cmd);
            Assert.False(result.IsValid);
        }

        [Fact]
        public void Validate_EmptyDocumentType_Fails()
        {
            var cmd = ValidCommand();
            cmd.DocumentType = "";

            var result = new UploadDocumentCommandValidator().Validate(cmd);
            Assert.False(result.IsValid);
        }
    }
}
