namespace MedVaultAPI.Models
{
    public class Appointment
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public string Doctor { get; set; } = string.Empty;
        public string Hospital { get; set; } = string.Empty;
        public string? Location { get; set; }
        public bool Visited { get; set; } = false;
        public string? VisitedDate { get; set; }
        public bool IsFollowUp { get; set; } = false;
        public string? FollowUpInterval { get; set; }
        public string? PreviousAppointmentId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User? User { get; set; }
    }
}