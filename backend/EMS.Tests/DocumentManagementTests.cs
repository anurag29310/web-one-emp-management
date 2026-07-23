using EMS.Application.Features.Documents.Commands;
using EMS.Infrastructure.Storage;
using EMS.Persistence.Context;
using EMS.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EMS.Tests
{
    public class DocumentManagementTests
    {
        [Fact]
        public async Task UploadDocument_SavesRecordAndFile()
        {
            var dbName = "ems_doc_test_" + Guid.NewGuid();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            var tempBase = Path.Combine(Path.GetTempPath(), "ems-tests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempBase);

            using var db = new ApplicationDbContext(options);
            var repo = new DocumentRepository(db);
            var storage = new LocalFileStorageService(tempBase);
            var logger = new NullLogger<EMS.Application.Features.Documents.Handlers.UploadDocumentCommandHandler>();

            var handler = new EMS.Application.Features.Documents.Handlers.UploadDocumentCommandHandler(repo, storage, logger);

            var employeeId = Guid.NewGuid();
            var cmd = new UploadDocumentCommand
            {
                EmployeeId = employeeId,
                DocumentType = "IDProof",
                FileName = "id.pdf",
                ContentType = "application/pdf",
                Content = new byte[] { 1, 2, 3 },
                UploadedBy = Guid.NewGuid()
            };

            var id = await handler.Handle(cmd, CancellationToken.None);

            var doc = db.EmployeeDocuments.FirstOrDefault(d => d.Id == id);
            Assert.NotNull(doc);

            var expectedPath = Path.Combine(tempBase, "Storage", "employee-documents", doc.BlobPath.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(expectedPath));

            // cleanup
            try { Directory.Delete(tempBase, true); } catch { }
        }
    }
}
