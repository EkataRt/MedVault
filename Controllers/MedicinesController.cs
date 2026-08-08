using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedVaultAPI.Data;
using MedVaultAPI.Models;
using MedVaultAPI.Services;
using System.Text.Json;

namespace MedVaultAPI.Controllers
{
    [ApiController]
    [Route("medicines")]
    public class MedicinesController : ControllerBase
    {
        private readonly MedVaultDbContext _db;
        private readonly SchedulingConflictService _conflictService;

        public MedicinesController(MedVaultDbContext db, SchedulingConflictService conflictService)
        {
            _db = db;
            _conflictService = conflictService;
        }

        private static Medicine MapToResponse(Medicine medicine)
        {
            medicine.Times = JsonSerializer.Deserialize<List<string>>(medicine.TimesJson) ?? new();
            medicine.LastTakenDates = JsonSerializer.Deserialize<List<string?>>(medicine.LastTakenDatesJson) ?? new();
            return medicine;
        }

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

        [HttpPost]
        public async Task<IActionResult> CreateMedicine([FromBody] Medicine medicine)
        {
            var userMedicines = await _db.Medicines
                .Where(m => m.UserId == medicine.UserId)
                .ToListAsync();

            if (_conflictService.HasMedicineConflict(medicine, userMedicines))
            {
                return Conflict(new { message = "Medication schedule conflicts or is too close to an existing medicine time." });
            }

            medicine.Id = Guid.NewGuid().ToString();
            medicine.CreatedAt = DateTime.UtcNow;

            medicine.TimesJson = JsonSerializer.Serialize(medicine.Times);
            medicine.LastTakenDatesJson = JsonSerializer.Serialize(medicine.LastTakenDates);

            _db.Medicines.Add(medicine);
            await _db.SaveChangesAsync();

            var notification = new MedicineNotification
            {
                Id = $"notif-{Guid.NewGuid()}",
                UserId = medicine.UserId,
                Title = "Medicine Added",
                Body = $"Your medicine {medicine.Name} has been scheduled.",
                Type = "medicine",
                ReferenceId = medicine.Id,
                DoseIndex = null,
                Read = false,
                CreatedAt = DateTime.UtcNow.ToString("O")
            };

            _db.MedicineNotification.Add(notification);
            await _db.SaveChangesAsync();

            return Ok(MapToResponse(medicine));
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateMedicine(string id, [FromBody] Medicine updated)
        {
            var medicine = await _db.Medicines.FindAsync(id);
            if (medicine == null) return NotFound();

            var testMedicine = new Medicine
            {
                Id = medicine.Id,
                UserId = medicine.UserId,
                Times = updated.Times ?? JsonSerializer.Deserialize<List<string>>(medicine.TimesJson ?? "[]")
            };

            var userMedicines = await _db.Medicines
                .Where(m => m.UserId == medicine.UserId)
                .ToListAsync();

            if (_conflictService.HasMedicineConflict(testMedicine, userMedicines))
            {
                return Conflict(new { message = "Updated medicine schedule conflicts with another dose time." });
            }

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