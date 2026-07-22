namespace MedVaultAPI.Models
{
    public class User
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        // Google Calendar integration
        public string? GoogleAccessToken { get; set; }
        public bool GoogleCalendarConnected { get; set; } = false;
        public long? GoogleTokenExpiry { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}