using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedVaultAPI.Data;
using MedVaultAPI.Models;
using System.Text.Json;

namespace MedVaultAPI.Controllers
{
    [ApiController]
    [Route("medicines")]
    public class MedicinesController : ControllerBase
    {
        private readonly MedVaultDbContext _db;

        public MedicinesController(MedVaultDbContext db)
        {
            _db = db;
        }

        private static Medicine MapToResponse(Medicine medicine)
        {
            medicine.Times = JsonSerializer.Deserialize<List<string>>(medicine.TimesJson) ?? new();
            medicine.LastTakenDates = JsonSerializer.Deserialize<List<string?>>(medicine.LastTakenDatesJson) ?? new();
            return medicine;
        }

        // GET /medicines?userId=x
        [HttpGet]
        public async Task<IActionResult> GetMedicines([FromQuery] string? userId)
        {
            var query = _db.Medicines.AsQueryable();
            if (!string.IsNullOrEmpty(userId))
                query = query.Where(m => m.UserId == userId);

            var medicines = await query.ToListAsync();
            return Ok(medicines.Select(MapToResponse));
        }

        // GET /medicines/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetMedicine(string id)
        {
            var medicine = await _db.Medicines.FindAsync(id);
            if (medicine == null) return NotFound();
            return Ok(MapToResponse(medicine));
        }

        // POST /medicines
        [HttpPost]
        public async Task<IActionResult> CreateMedicine([FromBody] Medicine medicine)
        {
            medicine.Id = Guid.NewGuid().ToString();
            medicine.CreatedAt = DateTime.UtcNow;
            medicine.TimesJson = JsonSerializer.Serialize(medicine.Times);
            medicine.LastTakenDatesJson = JsonSerializer.Serialize(medicine.LastTakenDates);

            _db.Medicines.Add(medicine);
            await _db.SaveChangesAsync();

            return Ok(MapToResponse(medicine));
        }

        // PATCH /medicines/{id}
        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateMedicine(string id, [FromBody] Medicine updated)
        {
            var medicine = await _db.Medicines.FindAsync(id);
            if (medicine == null) return NotFound();

            if (!string.IsNullOrEmpty(updated.Name)) medicine.Name = updated.Name;
            if (!string.IsNullOrEmpty(updated.Condition)) medicine.Condition = updated.Condition;
            if (!string.IsNullOrEmpty(updated.Dosage)) medicine.Dosage = updated.Dosage;
            if (!string.IsNullOrEmpty(updated.Frequency)) medicine.Frequency = updated.Frequency;
            if (!string.IsNullOrEmpty(updated.StartDate)) medicine.StartDate = updated.StartDate;
            if (!string.IsNullOrEmpty(updated.EndDate)) medicine.EndDate = updated.EndDate;
            if (!string.IsNullOrEmpty(updated.MealPreference)) medicine.MealPreference = updated.MealPreference;
            if (updated.TimesPerDay != 0) medicine.TimesPerDay = updated.TimesPerDay;
            if (updated.Times?.Any() == true)
                medicine.TimesJson = JsonSerializer.Serialize(updated.Times);
            if (updated.LastTakenDates?.Any() == true)
                medicine.LastTakenDatesJson = JsonSerializer.Serialize(updated.LastTakenDates);

            await _db.SaveChangesAsync();
            return Ok(MapToResponse(medicine));
        }

        // DELETE /medicines/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMedicine(string id)
        {
            var medicine = await _db.Medicines.FindAsync(id);
            if (medicine == null) return NotFound();

            _db.Medicines.Remove(medicine);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}