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
        [HttpGet]
        public async Task<IActionResult> GetDocuments(
    [FromQuery] string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return BadRequest("userId is required.");
            }

            var documents = await _db.MedDocument
                .Where(d => d.UserId == userId)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            return Ok(documents);
        }

        // GET /documents?userId=x
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDocument(
        string id,
        [FromQuery] string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return BadRequest("userId is required.");
            }

            var document = await _db.MedDocument
                .FirstOrDefaultAsync(d =>
                    d.Id == id &&
                    d.UserId == userId);

            if (document == null)
            {
                return NotFound();
            }

            return Ok(document);
        }



        [HttpGet("search")]
        public async Task<IActionResult> SearchDocuments(
     [FromQuery] string userId,
     [FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return BadRequest("userId is required.");
            }

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


        [HttpPost]
        public async Task<IActionResult> CreateDocument(
            [FromBody] MedDocument document)
        {
            if (string.IsNullOrWhiteSpace(document.UserId))
            {
                return BadRequest("userId is required.");
            }

            if (string.IsNullOrWhiteSpace(document.Name))
            {
                return BadRequest("Document name is required.");
            }

            if (string.IsNullOrWhiteSpace(document.FileName))
            {
                return BadRequest("fileName is required.");
            }
            var safeFileName = Path.GetFileName(document.FileName);

            if (safeFileName != document.FileName)
            {
                return BadRequest("Invalid file name.");
            }

            var filePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Uploads",
                safeFileName);

            if (!System.IO.File.Exists(filePath))
            {
                return BadRequest("Uploaded file does not exist.");
            }

            if (!string.IsNullOrWhiteSpace(document.FolderId))
            {
                var folder = await _db.Folder
                    .FirstOrDefaultAsync(f =>
                        f.Id == document.FolderId &&
                        f.UserId == document.UserId);

                if (folder == null)
                {
                    return BadRequest("Folder does not exist or does not belong to this user.");
                }
            }

            document.Id = Guid.NewGuid().ToString();

            if (string.IsNullOrWhiteSpace(document.CreatedAt))
            {
                document.CreatedAt = DateTime.UtcNow.ToString("O");
            }

            _db.MedDocument.Add(document);

            await _db.SaveChangesAsync();

            return Ok(document);
        }
        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateDocument(
    string id,
    [FromBody] MedDocument updated)
        {
            if (string.IsNullOrWhiteSpace(updated.UserId))
            {
                return BadRequest("userId is required.");
            }

            var document = await _db.MedDocument
                .FirstOrDefaultAsync(d =>
                    d.Id == id &&
                    d.UserId == updated.UserId);

            if (document == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrWhiteSpace(updated.Name))
            {
                document.Name = updated.Name;
            }

            await _db.SaveChangesAsync();

            return Ok(document);
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDocument(
     string id,
     [FromQuery] string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return BadRequest("userId is required.");
            }

            var document = await _db.MedDocument
                .FirstOrDefaultAsync(d =>
                    d.Id == id &&
                    d.UserId == userId);

            if (document == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrWhiteSpace(document.FileName))
            {
                var safeFileName = Path.GetFileName(document.FileName);

                var filePath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "Uploads",
                    safeFileName);

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
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

            var originalFileName = Path.GetFileName(file.FileName);
            var uniqueFileName = $"{Guid.NewGuid()}_{originalFileName}";
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
            var safeFileName = Path.GetFileName(fileName);

            if (string.IsNullOrWhiteSpace(safeFileName) ||
                safeFileName != fileName)
            {
                return BadRequest("Invalid file name.");
            }

            var filePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Uploads",
                safeFileName);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            System.IO.File.Delete(filePath);

            return NoContent();
        }
    }
}