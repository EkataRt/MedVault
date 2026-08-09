using MedVaultAPI.Data;
using MedVaultAPI.Model;
using MedVaultAPI.Models;
using MedVaultAPI.Services.Search;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace MedVaultAPI.Controllers
{
    [ApiController]
    [Route("documents")]
    public class DocumentsController : ControllerBase
    {
        private readonly MedVaultDbContext _db;
        private readonly IServiceScopeFactory _scopeFactory;

        // Injected IServiceScopeFactory for safe background job execution
        public DocumentsController(MedVaultDbContext db, IServiceScopeFactory scopeFactory)
        {
            _db = db;
            _scopeFactory = scopeFactory;
        }

        // GET /documents?userId=x
        [HttpGet]
        public async Task<IActionResult> GetDocuments([FromQuery] string? userId)
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
        public async Task<IActionResult> CreateDocument([FromBody] MedDocument document)
        {
            document.Id = Guid.NewGuid().ToString();

            if (string.IsNullOrEmpty(document.CreatedAt))
            {
                document.CreatedAt = DateTime.UtcNow.ToString("O");
            }

            document.ProcessingStatus = "Processing";

            _db.MedDocument.Add(document);
            await _db.SaveChangesAsync();

            // Queue OCR/extraction now that the single document row exists
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", document.FileName);
            _ = Task.Run(() => ProcessDocumentAsync(document.Id, filePath));

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
            var document = await _db.MedDocument.FindAsync(id);

            if (document == null)
            {
                return NotFound();
            }

            _db.MedDocument.Remove(document);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        // POST /documents/upload
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

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

            // No DB write here — this endpoint only persists the physical file.
            // The single MedDocument row is created by CreateDocument().
            return Ok(new { fileName = uniqueFileName, url = $"/uploads/{uniqueFileName}" });
        }

        // DELETE /documents/upload/{fileName}
        [HttpDelete("upload/{fileName}")]
        public IActionResult DeleteFile(string fileName)
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", fileName);

            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
                return NoContent();
            }

            return NotFound();
        }

        // GET /documents/smart-search?userId=x&query=kidney reports from last 5 months
        [HttpGet("smart-search")]
        public async Task<IActionResult> SmartSearch(
            [FromQuery] string userId,
            [FromQuery] string query,
            [FromServices] SmartSearchService smartSearchService)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Ok(new SmartSearchResult());
            }

            var results = await smartSearchService.SearchAsync(userId, query);
            return Ok(results);
        }

        // Background processing execution
        private async Task ProcessDocumentAsync(string documentId, string filePath)
        {
            // Use _scopeFactory instead of HttpContext
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MedVaultDbContext>();

            var doc = await db.MedDocument.FindAsync(documentId);
            if (doc == null) return;

            try
            {
                string extractedText = string.Empty;
                var extension = Path.GetExtension(filePath).ToLowerInvariant();

                if (extension == ".jpg" || extension == ".jpeg" || extension == ".png")
                {
                    using var engine = new Tesseract.TesseractEngine(@"./tessdata", "eng", Tesseract.EngineMode.Default);
                    using var img = Tesseract.Pix.LoadFromFile(filePath);
                    using var page = engine.Process(img);
                    extractedText = page.GetText();
                }
                else if (extension == ".txt")
                {
                    extractedText = await System.IO.File.ReadAllTextAsync(filePath);
                }

                doc.ExtractedText = extractedText;

                // Extract date
                var dateMatch = System.Text.RegularExpressions.Regex.Match(
                    extractedText,
                    @"\b(\d{4}[-/]\d{1,2}[-/]\d{1,2}|\d{1,2}[-/]\d{1,2}[-/]\d{4})\b"
                );

                if (dateMatch.Success && DateTime.TryParse(dateMatch.Value, out var parsedDate))
                {
                    doc.ReportDate = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
                }

                // Extract measurement (Creatinine pattern match)
                var creatinineMatch = System.Text.RegularExpressions.Regex.Match(
                    extractedText,
                    @"(?:serum\s+)?creatinine[:\s]+(\d+(?:\.\d+)?)\s*(mg/dL|mg/dl)?",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );

                if (creatinineMatch.Success && double.TryParse(creatinineMatch.Groups[1].Value, out var val))
                {
                    var measurement = new MedicalMeasurement
                    {
                        Id = Guid.NewGuid().ToString(),
                        DocumentId = doc.Id,
                        UserId = doc.UserId,
                        MeasurementType = "CREATININE",
                        Value = val,
                        Unit = string.IsNullOrWhiteSpace(creatinineMatch.Groups[2].Value) ? "mg/dL" : creatinineMatch.Groups[2].Value,
                        MeasuredDate = doc.ReportDate ?? DateTime.UtcNow
                    };

                    db.MedicalMeasurements.Add(measurement);
                }

                doc.ProcessingStatus = "Processed";
            }
            catch (Exception)
            {
                doc.ProcessingStatus = "Failed";
            }

            await db.SaveChangesAsync();
        }
    }
}