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


            await _db.SaveChangesAsync();


            return Ok(folder);
        }



        // DELETE /folders/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFolder(
            string id)
        {
            var folder = await _db.Folder.FindAsync(id);


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