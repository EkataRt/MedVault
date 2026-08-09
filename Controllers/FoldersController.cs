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


        [HttpGet]
        public async Task<IActionResult> GetFolders(
     [FromQuery] string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return BadRequest("userId is required.");
            }

            var folders = await _db.Folder
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            return Ok(folders);
        }



        [HttpPost]
        public async Task<IActionResult> CreateFolder(
     [FromBody] Folder folder)
        {
            if (string.IsNullOrWhiteSpace(folder.UserId))
            {
                return BadRequest("userId is required.");
            }

            if (string.IsNullOrWhiteSpace(folder.Name))
            {
                return BadRequest("Folder name is required.");
            }

            if (!string.IsNullOrWhiteSpace(folder.ParentId))
            {
                var parentFolder = await _db.Folder
                    .FirstOrDefaultAsync(f =>
                        f.Id == folder.ParentId &&
                        f.UserId == folder.UserId);

                if (parentFolder == null)
                {
                    return BadRequest("Parent folder does not exist.");
                }
            }

            folder.Id = Guid.NewGuid().ToString();

            if (string.IsNullOrWhiteSpace(folder.CreatedAt))
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
            if (string.IsNullOrWhiteSpace(updated.UserId))
            {
                return BadRequest("userId is required.");
            }

            var folder = await _db.Folder
                .FirstOrDefaultAsync(f =>
                    f.Id == id &&
                    f.UserId == updated.UserId);

            if (folder == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrWhiteSpace(updated.Name))
            {
                folder.Name = updated.Name;
            }

            await _db.SaveChangesAsync();

            return Ok(folder);
        }



        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFolder(
      string id,
      [FromQuery] string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return BadRequest("userId is required.");
            }

            var folder = await _db.Folder
                .FirstOrDefaultAsync(f =>
                    f.Id == id &&
                    f.UserId == userId);

            if (folder == null)
            {
                return NotFound();
            }

            _db.Folder.Remove(folder);

            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}