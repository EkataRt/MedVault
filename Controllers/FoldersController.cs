using MedVaultAPI.Data;
using MedVaultAPI.Model;
using MedVaultAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedVaultAPI.Controllers
{
    [ApiController]
    [Route("folders")]
    public class FoldersController : ControllerBase
    {
        private readonly MedVaultDbContext _db;

        public FoldersController(MedVaultDbContext db)
        {
            _db = db;
        }

        // GET /folders?userId=x
        [HttpGet]
        public async Task<IActionResult> GetFolders(
            [FromQuery] string? userId)
        {
            var query = _db.Folder.AsQueryable();

            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(f => f.UserId == userId);
            }

            var folders = await query
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            return Ok(folders);
        }

        // POST /folders
        [HttpPost]
        public async Task<IActionResult> CreateFolder(
            [FromBody] Folder folder)
        {
            if (folder == null || string.IsNullOrWhiteSpace(folder.Name))
            {
                return BadRequest("Name is required.");
            }

            // No DB constraint anymore - validate manually.
            if (string.IsNullOrWhiteSpace(folder.ParentId))
            {
                folder.ParentId = null;
            }
            else
            {
                var parentExists = await _db.Folder.AnyAsync(
                    f => f.Id == folder.ParentId && f.UserId == folder.UserId);

                if (!parentExists)
                {
                    return BadRequest("ParentId is invalid or does not belong to this user.");
                }
            }

            folder.Id = Guid.NewGuid().ToString();

            if (string.IsNullOrEmpty(folder.CreatedAt))
            {
                folder.CreatedAt = DateTime.UtcNow.ToString("O");
            }

            _db.Folder.Add(folder);
            await _db.SaveChangesAsync();

            return Ok(folder);
        }

        // PATCH /folders/{id}
        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateFolder(
            string id,
            [FromBody] Folder updated)
        {
            var folder = await _db.Folder.FindAsync(id);

            if (folder == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(updated.Name))
            {
                folder.Name = updated.Name;
            }

            if (updated.ParentId != null)
            {
                if (string.IsNullOrWhiteSpace(updated.ParentId))
                {
                    folder.ParentId = null;
                }
                else
                {
                    if (updated.ParentId == id)
                    {
                        return BadRequest("A folder cannot be its own parent.");
                    }

                    var parentExists = await _db.Folder.AnyAsync(
                        f => f.Id == updated.ParentId && f.UserId == folder.UserId);

                    if (!parentExists)
                    {
                        return BadRequest("ParentId is invalid or does not belong to this user.");
                    }

                    folder.ParentId = updated.ParentId;
                }
            }

            await _db.SaveChangesAsync();

            return Ok(folder);
        }

        // DELETE /folders/{id}
        // No DB cascade exists anymore - this explicitly walks the whole tree:
        // subfolders (recursively) -> their documents -> those documents' topics/
        // measurements -> physical files, then the folders themselves.
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFolder(string id)
        {
            var folder = await _db.Folder.FindAsync(id);

            if (folder == null)
            {
                return NotFound();
            }

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var allFolderIds = new List<string>();
                await CollectFolderIdsRecursive(id, allFolderIds);
                allFolderIds.Add(id);

                var documents = await _db.MedDocument
                    .Where(d => allFolderIds.Contains(d.FolderId))
                    .ToListAsync();

                var documentIds = documents.Select(d => d.Id).ToList();
                var fileNames = documents.Select(d => d.FileName).ToList();

                _db.MedicalTopic.RemoveRange(
                    _db.MedicalTopic.Where(t => documentIds.Contains(t.DocumentId)));

                _db.MedicalMeasurement.RemoveRange(
                    _db.MedicalMeasurement.Where(m => documentIds.Contains(m.DocumentId)));

                _db.MedDocument.RemoveRange(documents);

                var foldersToRemove = await _db.Folder
                    .Where(f => allFolderIds.Contains(f.Id))
                    .ToListAsync();
                _db.Folder.RemoveRange(foldersToRemove);

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                var uploadsDir = Path.GetFullPath(
                    Path.Combine(Directory.GetCurrentDirectory(), "Uploads"));

                foreach (var fileName in fileNames)
                {
                    if (string.IsNullOrWhiteSpace(fileName)) continue;

                    var safeFileName = Path.GetFileName(fileName);
                    var fullFilePath = Path.GetFullPath(Path.Combine(uploadsDir, safeFileName));

                    if (fullFilePath.StartsWith(uploadsDir, StringComparison.OrdinalIgnoreCase)
                        && System.IO.File.Exists(fullFilePath))
                    {
                        System.IO.File.Delete(fullFilePath);
                    }
                }
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            return NoContent();
        }

        private async Task CollectFolderIdsRecursive(string parentId, List<string> collected)
        {
            var childIds = await _db.Folder
                .Where(f => f.ParentId == parentId)
                .Select(f => f.Id)
                .ToListAsync();

            foreach (var childId in childIds)
            {
                collected.Add(childId);
                await CollectFolderIdsRecursive(childId, collected);
            }
        }
    }
}