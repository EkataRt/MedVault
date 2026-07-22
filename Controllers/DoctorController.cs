using MedVaultAPI.Data;
using MedVaultAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using MedVaultAPI.Services;

namespace MedVaultAPI.Controllers
{
    [ApiController]
    [Route("doctor")]
    public class DoctorController : ControllerBase
    {
        private readonly MedVaultDbContext _db;
        private readonly AIQueryService _aiService;

        public DoctorController(MedVaultDbContext db, AIQueryService aiService)
        {
            _db = db;
            _aiService = aiService;
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchDoctor(
            [FromQuery] string location,
            [FromQuery] string? healthProblem,
            [FromQuery] string? hospitalType,
            [FromQuery] bool isSort = false)
        {
            string? specialty = null;

            if (!string.IsNullOrWhiteSpace(healthProblem))
            {
                specialty = await _aiService.ClassifySpecialtyAsync(healthProblem);
            }

            var query = _db.Doctor.Where(d => d.Location == location);

            if (!string.IsNullOrWhiteSpace(specialty))
            {
                query = query.Where(d => d.Specialty == specialty);
            }

            if (!string.IsNullOrWhiteSpace(hospitalType) && hospitalType != "all")
            {
                query = query.Where(d => d.HospitalType == hospitalType);
            }

            var doctors = await query.ToListAsync();

            if (isSort)
            {
                // TODO: Place-based Haversine sort, as discussed earlier
            }

            return Ok(doctors);
        }

        [HttpGet("locations")]
        public async Task<IActionResult> GetLocations()
        {
            var locations = await _db.Place
                .OrderBy(p => p.Name)
                .Select(p => p.Name)
                .ToListAsync();

            return Ok(locations);
        }
    }
}