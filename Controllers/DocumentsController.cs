using MedVaultAPI.Data;
using MedVaultAPI.Model;
using MedVaultAPI.Models;
using MedVaultAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedVaultAPI.Controllers
{
    [ApiController]
    [Route("documents")]
    public class DocumentsController : ControllerBase
    {
        private readonly MedVaultDbContext _db;
        private readonly ReportAnalysisService _reportAnalysisService;
        private readonly SmartSearchService _smartSearchService;
        public DocumentsController(
            MedVaultDbContext db,
            ReportAnalysisService reportAnalysisService,
            SmartSearchService smartSearchService)
        {
            _db = db;
            _reportAnalysisService = reportAnalysisService;
            _smartSearchService = smartSearchService;
        }
        // GET /documents?userId=x
        [HttpGet]
        public async Task<IActionResult> GetDocuments(
            [FromQuery] string? userId)
        {
            var query = _db.MedDocument.AsQueryable();
            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(d => d.UserId == userId);
            }
            var documents = await query
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
            return Ok(documents);
        }
        // GET /documents/search?userId=x&query=blood
        // Existing simple filename/document-name search. Unchanged.
        [HttpGet("search")]
        public async Task<IActionResult> SearchDocuments(
            [FromQuery] string userId,
            [FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Ok(new List<MedDocument>());
            }
            var documents = await _db.MedDocument
                .Where(d => d.UserId == userId)
                .ToListAsync();
            var results = documents
                .Where(d =>
                    (!string.IsNullOrEmpty(d.Name) &&
                     d.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                    ||
                    (!string.IsNullOrEmpty(d.FileName) &&
                     d.FileName.Contains(query, StringComparison.OrdinalIgnoreCase))
                )
                .OrderBy(d => d.Name)
                .ToList();
            return Ok(results);
        }
        // GET /documents/smart-search?userId=x&query=kidney reports from 2026
        // New Smart Search endpoint - separate from the simple search above.
        [HttpGet("smart-search")]
        public async Task<IActionResult> SmartSearch(
            [FromQuery] string userId,
            [FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Ok(new List<SmartSearchResult>());
            }
            var results = await _smartSearchService.SearchAsync(userId, query);
            return Ok(results);
        }
        // GET /documents/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDocument(string id)
        {
            var document = await _db.MedDocument.FindAsync(id);
            if (document == null)
            {
                return NotFound();
            }
            return Ok(document);
        }
        // POST /documents
        [HttpPost]
        public async Task<IActionResult> CreateDocument(
    [FromBody] MedDocument document)
        {
            if (document == null || string.IsNullOrWhiteSpace(document.UserId))
            {
                return BadRequest("UserId is required.");
            }

            if (string.IsNullOrWhiteSpace(document.Name))
            {
                return BadRequest("Name is required.");
            }

            // No DB constraint enforces this anymore - this check is now the only
            // safeguard against an orphaned/invalid FolderId.
            if (!string.IsNullOrWhiteSpace(document.FolderId))
            {
                var folderExists = await _db.Folder.AnyAsync(
                    f => f.Id == document.FolderId && f.UserId == document.UserId);

                if (!folderExists)
                {
                    return BadRequest("FolderId is invalid or does not belong to this user.");
                }
            }

            document.Id = Guid.NewGuid().ToString();

            if (string.IsNullOrEmpty(document.CreatedAt))
            {
                document.CreatedAt = DateTime.UtcNow.ToString("O");
            }

            _db.MedDocument.Add(document);
            await _db.SaveChangesAsync();

            try
            {
                await _reportAnalysisService.AnalyzeDocumentAsync(document);
            }
            catch
            {
            }

            return Ok(document);
        }
        // PATCH /documents/{id}
        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateDocument(
            string id,
            [FromBody] MedDocument updated)
        {
            var document = await _db.MedDocument.FindAsync(id);
            if (document == null)
            {
                return NotFound();
            }
            if (!string.IsNullOrEmpty(updated.Name))
            {
                document.Name = updated.Name;
            }
            await _db.SaveChangesAsync();
            return Ok(document);
        }
        // DELETE /documents/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDocument(string id)
        {
            var document = await _db.MedDocument.FirstOrDefaultAsync(d => d.Id == id);

            if (document == null)
            {
                return NotFound();
            }

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                _db.MedicalTopic.RemoveRange(_db.MedicalTopic.Where(t => t.DocumentId == id));
                _db.MedicalMeasurement.RemoveRange(_db.MedicalMeasurement.Where(m => m.DocumentId == id));
                _db.MedDocument.Remove(document);

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            DeletePhysicalFile(document.FileName);

            return NoContent();
        }
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }
            // Define the folder where files will be stored locally
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }
            var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            // Return the properties expected by your Angular service
            return Ok(new
            {
                fileName = uniqueFileName,
                url = $"{Request.Scheme}://{Request.Host}/uploads/{uniqueFileName}"
            });
        }

        [HttpDelete("upload/{fileName}")]
        public IActionResult DeleteFile(string fileName)
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", fileName);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
            return NoContent(); 
        }
        // POST /documents/{id}/analyze?userId=x
        // Manual trigger for report analysis - primarily for V1 testing.
        [HttpPost("{id}/analyze")]
        public async Task<IActionResult> AnalyzeDocument(
            string id,
            [FromQuery] string userId)
        {
            var document = await _db.MedDocument
                .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);
            if (document == null)
            {
                return NotFound();
            }
            await _reportAnalysisService.AnalyzeDocumentAsync(document);
            return Ok(document);
        }
        // Shared helper so DeleteDocument and DeleteFile behave identically and
        // consistently sanitize/contain the path the same way.
        private bool DeletePhysicalFile(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return false;
            }

            var uploadsDir = Path.GetFullPath(
                Path.Combine(Directory.GetCurrentDirectory(), "Uploads"));

            var safeFileName = Path.GetFileName(fileName);
            var fullFilePath = Path.GetFullPath(Path.Combine(uploadsDir, safeFileName));

            if (!fullFilePath.StartsWith(uploadsDir, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (System.IO.File.Exists(fullFilePath))
            {
                System.IO.File.Delete(fullFilePath);
                return true;
            }

            return false;
        }
        [HttpGet("reporttypes")]
        public async Task<IActionResult> ReportTypes(
           [FromQuery] string userId,string? FolderId)
        {
            List<string> results = new List<string>();

            if (!string.IsNullOrWhiteSpace(FolderId))
            {
                results = await _db.MedDocument.Where(d => d.UserId == userId && d.FolderId == FolderId && d.ReportType != null)
         .Select(d => d.ReportType!).Distinct().ToListAsync();
                return Ok(results);
            }
            results = await _db.MedDocument.Where(d => d.UserId == userId && d.ReportType != null)
       .Select(d => d.ReportType!).Distinct().ToListAsync();
            return Ok(results);

        }
        [HttpGet("topics")]
        public async Task<IActionResult> Topics(
           [FromQuery] string userId)
        {
            List<string> results = new List<string>();
            results = await _db.MedicalTopic.Where(d => d.UserId == userId && d.Topic != null)
         .Select(d => d.Topic!).Distinct().ToListAsync();

            return Ok(results);


        }
    }
}