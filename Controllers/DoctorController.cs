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
            const double RadiusMiles = 5.0;

            // 1. Resolve the searched location's coordinates
            var origin = await _db.Place.FirstOrDefaultAsync(p => p.Name == location);
            if (origin == null)
            {
                return BadRequest($"Unknown location: {location}");
            }

            // 2. Resolve AI specialty (if a health problem was given)
            string? specialty = null;
            if (!string.IsNullOrWhiteSpace(healthProblem))
            {
                specialty = await _aiService.ClassifySpecialtyAsync(healthProblem);
            }

            // 3. Apply non-location filters first (specialty, hospital type)
            var query = _db.Doctor.AsQueryable();
            if (!string.IsNullOrWhiteSpace(specialty))
            {
                query = query.Where(d => d.Specialty == specialty);
            }
            if (!string.IsNullOrWhiteSpace(hospitalType) && hospitalType != "all")
            {
                query = query.Where(d => d.HospitalType == hospitalType);
            }
            var candidates = await query.ToListAsync();

            // 4. Resolve coordinates for every distinct doctor location in one query
            var locationNames = candidates.Select(d => d.Location).Distinct().ToList();
            var places = await _db.Place
                .Where(p => locationNames.Contains(p.Name))
                .ToDictionaryAsync(p => p.Name, p => p);

            // 5. Filter to doctors within 5 miles, keeping the computed distance for sorting
            var withinRadius = candidates
                .Select(d => new
                {
                    Doctor = d,
                    DistanceMiles = places.TryGetValue(d.Location, out var place)
                        ? HaversineMiles(origin.Latitude, origin.Longitude, place.Latitude, place.Longitude)
                        : (double?)null,
                })
                .Where(x => x.DistanceMiles.HasValue && x.DistanceMiles.Value <= RadiusMiles)
                .ToList();

            // 6. Sort by distance if requested, otherwise keep default order
            var result = isSort
                ? withinRadius.OrderBy(x => x.DistanceMiles).Select(x => x.Doctor)
                : withinRadius.Select(x => x.Doctor);

            return Ok(result.ToList());
        }

        private static double HaversineMiles(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 3958.8; // Earth radius in miles
            double dLat = ToRad(lat2 - lat1);
            double dLon = ToRad(lon2 - lon1);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                     + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
                     * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private static double ToRad(double deg) => deg * Math.PI / 180;

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