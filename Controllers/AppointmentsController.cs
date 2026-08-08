using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedVaultAPI.Data;
using MedVaultAPI.Models;

namespace MedVaultAPI.Controllers
{
    [ApiController]
    [Route("appointments")]
    public class AppointmentsController : ControllerBase
    {
        private readonly MedVaultDbContext _db;

        public AppointmentsController(MedVaultDbContext db)
        {
            _db = db;
        }

        // GET /appointments?userId=x
        [HttpGet]
        public async Task<IActionResult> GetAppointments([FromQuery] string? userId)
        {
            var query = _db.Appointments.AsQueryable();
            if (!string.IsNullOrEmpty(userId))
                query = query.Where(a => a.UserId == userId);

            return Ok(await query.ToListAsync());
        }

        // GET /appointments/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAppointment(string id)
        {
            var appointment = await _db.Appointments.FindAsync(id);
            if (appointment == null) return NotFound();
            return Ok(appointment);
        }

        // POST /appointments
        [HttpPost]
        public async Task<IActionResult> CreateAppointment([FromBody] Appointment appointment)
        {
            appointment.Id = Guid.NewGuid().ToString();
            appointment.CreatedAt = DateTime.UtcNow;

            _db.Appointments.Add(appointment);
            await _db.SaveChangesAsync();

            return Ok(appointment);
        }

        // PATCH /appointments/{id}
        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateAppointment(string id, [FromBody] Appointment updated)
        {
            var appointment = await _db.Appointments.FindAsync(id);
            if (appointment == null) return NotFound();

            if (!string.IsNullOrEmpty(updated.Title)) appointment.Title = updated.Title;
            if (!string.IsNullOrEmpty(updated.Category)) appointment.Category = updated.Category;
            if (!string.IsNullOrEmpty(updated.Date)) appointment.Date = updated.Date;
            if (!string.IsNullOrEmpty(updated.Time)) appointment.Time = updated.Time;
            if (!string.IsNullOrEmpty(updated.Doctor)) appointment.Doctor = updated.Doctor;
            if (!string.IsNullOrEmpty(updated.Hospital)) appointment.Hospital = updated.Hospital;
            if (updated.Location != null) appointment.Location = updated.Location;
            if (updated.VisitedDate != null) appointment.VisitedDate = updated.VisitedDate;
            if (updated.FollowUpInterval != null) appointment.FollowUpInterval = updated.FollowUpInterval;
            if (updated.PreviousAppointmentId != null) appointment.PreviousAppointmentId = updated.PreviousAppointmentId;

            appointment.Visited = updated.Visited;
            appointment.IsFollowUp = updated.IsFollowUp;

            await _db.SaveChangesAsync();
            return Ok(appointment);
            var notification = new MedicineNotification
            {
                Id = $"notif-{Guid.NewGuid()}",
                UserId = appointment.UserId,
                Title = "Medicine Added",
                Body = $"Your medicine {appointment.Title} has been scheduled.",
                Type = "medicine",
                ReferenceId = appointment.Id,
                DoseIndex = null,
                Read = false,
                CreatedAt = DateTime.UtcNow.ToString("O")
            };

            _db.MedicineNotification.Add(notification);
            await _db.SaveChangesAsync();
        }

        // DELETE /appointments/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAppointment(string id)
        {
            var appointment = await _db.Appointments.FindAsync(id);
            if (appointment == null) return NotFound();

            _db.Appointments.Remove(appointment);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}