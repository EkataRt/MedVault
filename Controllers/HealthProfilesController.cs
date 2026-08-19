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


        private static HealthProfile MapToResponse(HealthProfile profile)
        {
            profile.Allergies = JsonSerializer.Deserialize<List<string>>(
                profile.AllergiesJson, _jsonOptions) ?? new();
            return profile;
        }
        private static float ComputeBmi(float heightCm, float weightKg)
        {
            if (heightCm <= 0 || weightKg <= 0) return 0;
            var heightM = heightCm / 100f;
            return (float)Math.Round(weightKg / (heightM * heightM), 2);
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
            profile.Bmi = ComputeBmi(profile.Height, profile.Weight);

            _db.HealthProfiles.Add(profile);
            await _db.SaveChangesAsync();

            if (profile.Bmi > 0)
            {
                _db.BmiHistories.Add(new BmiHistory
                {
                    ProfileId = profile.Id,
                    Height = profile.Height,
                    Weight = profile.Weight,
                    Bmi = profile.Bmi,
                    RecordedAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();
            }

            return Ok(MapToResponse(profile));
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateHealthProfile(string id, [FromBody] HealthProfile updated)
        {
            var profile = await _db.HealthProfiles.FindAsync(id);
            if (profile == null) return NotFound();

            if (!string.IsNullOrEmpty(updated.FullName)) profile.FullName = updated.FullName;
            if (!string.IsNullOrEmpty(updated.BloodType)) profile.BloodType = updated.BloodType;
            if (!string.IsNullOrEmpty(updated.Sex)) profile.Sex = updated.Sex;
            if (!string.IsNullOrEmpty(updated.LastCheckup)) profile.LastCheckup = updated.LastCheckup;
            if (updated.DateOfBirth != default) profile.DateOfBirth = updated.DateOfBirth;

            var heightOrWeightChanged = false;

            if (updated.Height > 0 && updated.Height != profile.Height)
            {
                profile.Height = updated.Height;
                heightOrWeightChanged = true;
            }

            if (updated.Weight > 0 && updated.Weight != profile.Weight)
            {
                profile.Weight = updated.Weight;
                heightOrWeightChanged = true;
            }

            profile.AllergiesJson = JsonSerializer.Serialize(updated.Allergies ?? new(), _jsonOptions);

            if (heightOrWeightChanged)
                profile.Bmi = ComputeBmi(profile.Height, profile.Weight);

            await _db.SaveChangesAsync();

            if (heightOrWeightChanged && profile.Bmi > 0)
            {
                _db.BmiHistories.Add(new BmiHistory
                {
                    ProfileId = profile.Id,
                    Height = profile.Height,
                    Weight = profile.Weight,
                    Bmi = profile.Bmi,
                    RecordedAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();
            }

            return Ok(MapToResponse(profile));
        }

        // GET /healthProfiles/{id}/bmiHistory
        [HttpGet("{id}/bmiHistory")]
        public async Task<IActionResult> GetBmiHistory(string id)
        {
            var exists = await _db.HealthProfiles.AnyAsync(h => h.Id == id);
            if (!exists) return NotFound();

            var history = await _db.BmiHistories
                .Where(b => b.ProfileId == id)
                .OrderBy(b => b.RecordedAt)
                .ToListAsync();

            return Ok(history);
        }

        // DELETE /healthProfiles/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteHealthProfile(string id)
        {
            var profile = await _db.HealthProfiles.FindAsync(id);
            if (profile == null) return NotFound();

            // No cascade delete exists since there's no FK — clean up manually
            var relatedHistory = await _db.BmiHistories
                .Where(b => b.ProfileId == id)
                .ToListAsync();
            _db.BmiHistories.RemoveRange(relatedHistory);

            _db.HealthProfiles.Remove(profile);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}