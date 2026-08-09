using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedVaultAPI.Data;
using MedVaultAPI.Models;
using System.Text.Json;

namespace MedVaultAPI.Controllers
{
    [ApiController]
    [Route("healthProfiles")]
    public class HealthProfilesController : ControllerBase
    {
        private readonly MedVaultDbContext _db;
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public HealthProfilesController(MedVaultDbContext db)
        {
            _db = db;
        }

        // Converts DB JSON strings back to arrays for Angular
        private static HealthProfile MapToResponse(HealthProfile profile)
        {
            profile.Allergies = JsonSerializer.Deserialize<List<string>>(
                profile.AllergiesJson, _jsonOptions) ?? new();
            return profile;
        }

        // GET /healthProfiles?userId=x
        [HttpGet]
        public async Task<IActionResult> GetHealthProfiles([FromQuery] string? userId)
        {
            var query = _db.HealthProfiles.AsQueryable();

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(h => h.UserId == userId);

            var profiles = await query.ToListAsync();
            return Ok(profiles.Select(MapToResponse));
        }

        // GET /healthProfiles/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetHealthProfile(string id)
        {
            var profile = await _db.HealthProfiles.FindAsync(id);
            if (profile == null) return NotFound();
            return Ok(MapToResponse(profile));
        }

        // POST /healthProfiles
        // Angular sends: { userId, age, allergies[], bloodType, fullName, height, lastCheckup, sex, weight }
        [HttpPost]
        public async Task<IActionResult> CreateHealthProfile([FromBody] HealthProfile profile)
        {
            profile.Id = Guid.NewGuid().ToString();
            profile.CreatedAt = DateTime.UtcNow;
            profile.AllergiesJson = JsonSerializer.Serialize(profile.Allergies, _jsonOptions);

            _db.HealthProfiles.Add(profile);
            await _db.SaveChangesAsync();

            return Ok(MapToResponse(profile));
        }

        // PATCH /healthProfiles/{id}
        // Angular sends: { age, allergies[], bloodType, fullName, height, lastCheckup, sex, weight }
        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateHealthProfile(string id, [FromBody] HealthProfile updated)
        {
            var profile = await _db.HealthProfiles.FindAsync(id);
            if (profile == null) return NotFound();

            // Update all fields Angular sends from the edit-profile form
            if (!string.IsNullOrEmpty(updated.FullName)) profile.FullName = updated.FullName;
            if (!string.IsNullOrEmpty(updated.BloodType)) profile.BloodType = updated.BloodType;
            if (!string.IsNullOrEmpty(updated.Sex)) profile.Sex = updated.Sex;
            if (!string.IsNullOrEmpty(updated.LastCheckup)) profile.LastCheckup = updated.LastCheckup;
            if (updated.DateOfBirth != default) profile.DateOfBirth = updated.DateOfBirth;
            if (updated.Height > 0) profile.Height = updated.Height;
            if (updated.Weight > 0) profile.Weight = updated.Weight;

            // Allergies — always update even if empty array (user may clear them)
            profile.AllergiesJson = JsonSerializer.Serialize(updated.Allergies ?? new(), _jsonOptions);

            await _db.SaveChangesAsync();
            return Ok(MapToResponse(profile));
        }

        // DELETE /healthProfiles/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteHealthProfile(string id)
        {
            var profile = await _db.HealthProfiles.FindAsync(id);
            if (profile == null) return NotFound();

            _db.HealthProfiles.Remove(profile);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}