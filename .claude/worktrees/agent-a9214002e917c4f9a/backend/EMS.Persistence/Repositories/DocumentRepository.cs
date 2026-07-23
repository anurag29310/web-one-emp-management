using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EMS.Persistence.Repositories
{
    public class DocumentRepository : IDocumentRepository
    {
        private readonly ApplicationDbContext _db;

        public DocumentRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(EmployeeDocument doc)
        {
            await _db.EmployeeDocuments.AddAsync(doc);
        }

        public async Task<EmployeeDocument?> GetByIdAsync(Guid id)
            => await _db.EmployeeDocuments.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

        public async Task<IEnumerable<EmployeeDocument>> GetForEmployeeAsync(Guid employeeId, int page, int pageSize, string? search, string? documentType)
        {
            var q = _db.EmployeeDocuments.AsNoTracking().Where(d => d.EmployeeId == employeeId && !d.IsDeleted);
            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(d => d.OriginalFileName.Contains(search));
            if (!string.IsNullOrWhiteSpace(documentType))
                q = q.Where(d => d.DocumentType == documentType);

            return await q.OrderByDescending(d => d.UploadedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task SoftDeleteAsync(EmployeeDocument doc)
        {
            doc.IsDeleted = true;
            doc.DeletedAtUtc = DateTime.UtcNow;
            _db.EmployeeDocuments.Update(doc);
        }

        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }
    }
}
