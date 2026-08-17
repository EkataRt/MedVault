using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedVaultAPI.Data;
using MedVaultAPI.Models;

namespace MedVaultAPI.Controllers
{
    [ApiController]
    [Route("notifications")]
    public class NotificationsController : ControllerBase
    {
        private readonly MedVaultDbContext _db;

        public NotificationsController(MedVaultDbContext db)
        {
            _db = db;
        }


        // GET /notifications?userId=x
        [HttpGet]
        public async Task<IActionResult> GetNotifications(
            [FromQuery] string? userId)
        {
            var query = _db.MedicineNotification.AsQueryable();


            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(n => n.UserId == userId);
            }


            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();


            return Ok(notifications);
        }



        // GET /notifications/in-app/{userId}
        [HttpGet("in-app/{userId}")]
        public async Task<IActionResult> GetInAppNotifications(
            string userId)
        {

            var notifications = await _db.MedicineNotification
                .Where(n => n.UserId == userId)
                .OrderBy(n => n.Read)              
                .ThenByDescending(n => n.CreatedAt) 
                .Take(10)
                .ToListAsync();


            return Ok(notifications);
        }




        // POST /notifications
        [HttpPost]
        public async Task<IActionResult> CreateNotification(
            [FromBody] MedicineNotification notification)
        {

            if (string.IsNullOrEmpty(notification.Id))
            {
                notification.Id =
                    $"notif-{Guid.NewGuid()}";
            }


            if (string.IsNullOrEmpty(notification.CreatedAt))
            {
                notification.CreatedAt =
                    DateTime.UtcNow.ToString("O");
            }


            _db.MedicineNotification.Add(notification);

            await _db.SaveChangesAsync();


            return Ok(notification);
        }





        // PATCH /notifications/{id}
        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateNotification(
            string id,
            [FromBody] MedicineNotification updated)
        {

            var notification =
                await _db.MedicineNotification
                .FindAsync(id);


            if (notification == null)
                return NotFound();



            notification.Read = updated.Read;


            await _db.SaveChangesAsync();


            return Ok(notification);
        }





        // PATCH /notifications/read-all/{userId}
        [HttpPatch("read-all/{userId}")]
        public async Task<IActionResult> MarkAllAsRead(
            string userId)
        {

            var notifications =
                await _db.MedicineNotification
                .Where(n =>
                    n.UserId == userId &&
                    !n.Read)
                .ToListAsync();



            foreach (var notification in notifications)
            {
                notification.Read = true;
            }



            await _db.SaveChangesAsync();


            return Ok(new
            {
                message = "All notifications marked as read"
            });
        }





        // DELETE /notifications/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotification(
            string id)
        {

            var notification =
                await _db.MedicineNotification
                .FindAsync(id);



            if (notification == null)
                return NotFound();



            _db.MedicineNotification.Remove(notification);


            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return NoContent();
            }

            return NoContent();
        }

    }
}