using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedVaultAPI.Data;
using MedVaultAPI.Models;
using MedVaultAPI.Models.DTOs;
using MedVaultAPI.Services;

namespace MedVaultAPI.Controllers
{
    [ApiController]
    [Route("users")]
    public class UsersController : ControllerBase
    {
        private readonly MedVaultDbContext _db;
        private readonly JwtService _jwt;

        public UsersController(MedVaultDbContext db, JwtService jwt)
        {
            _db = db;
            _jwt = jwt;
        }

        // LOGIN: POST /users/login  ← new dedicated login endpoint
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
                return Unauthorized(new { message = "Invalid username or password" });

            var token = _jwt.GenerateToken(user);

            return Ok(new
            {
                id = user.Id,
                username = user.Username,
                email = user.Email,
                token = token
            });
        }

        // SIGNUP CHECK: GET /users?email=x  ← checks if email already exists
        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery] string? email)
        {
            if (!string.IsNullOrEmpty(email))
            {
                var exists = await _db.Users.AnyAsync(u => u.Email == email);
                if (exists)
                    return Ok(new[] { new { email } }); // non-empty = user exists
                return Ok(new List<object>());           // empty = email is free
            }

            return Ok(new List<object>());
        }

        // SIGNUP: POST /users
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] SignupRequest request)
        {
            var existingUser = await _db.Users
                .AnyAsync(u => u.Email == request.Email || u.Username == request.Username);

            if (existingUser)
                return Conflict(new { message = "User already exists" });

            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                Username = request.Username,
                Email = request.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return Ok(new { id = user.Id, username = user.Username, email = user.Email });
        }

        // UPDATE: PATCH /users/{id}
        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserRequest request)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            if (!string.IsNullOrEmpty(request.Username)) user.Username = request.Username;
            if (!string.IsNullOrEmpty(request.Email)) user.Email = request.Email;
            if (!string.IsNullOrEmpty(request.Password))
                user.Password = BCrypt.Net.BCrypt.HashPassword(request.Password);

            await _db.SaveChangesAsync();

            return Ok(new { id = user.Id, username = user.Username, email = user.Email });
        }

        // DELETE: DELETE /users/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }

    public class UpdateUserRequest
    {
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
    }
}