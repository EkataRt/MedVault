using MedVaultAPI.Data;
using MedVaultAPI.Model;
using MedVaultAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedVaultAPI.Controllers
{
    [ApiController]
    [Route("documents")]
    public class DocumentsController : ControllerBase
    {
        private readonly MedVaultDbContext _db;

        public DocumentsController(MedVaultDbContext db)
        {
            _db = db;
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
        public async Task<IActionResult> CreateDocument(
            [FromBody] MedDocument document)
        {
            document.Id = Guid.NewGuid().ToString();


            if (string.IsNullOrEmpty(document.CreatedAt))
            {
                document.CreatedAt = DateTime.UtcNow.ToString("O");
            }


            _db.MedDocument.Add(document);

            await _db.SaveChangesAsync();


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
                url = $"/uploads/{uniqueFileName}"
            });
        }

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
    }
}