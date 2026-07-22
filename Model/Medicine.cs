using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace MedVaultAPI.Models
{
    public class Medicine
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public string Dosage { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public string MealPreference { get; set; } = string.Empty; // 'before' | 'after' | 'any'
        public int TimesPerDay { get; set; }

        // Arrays — stored as JSON in DB
        [NotMapped]
        public List<string> Times { get; set; } = new();
        public string TimesJson { get; set; } = "[]";

        [NotMapped]
        public List<string?> LastTakenDates { get; set; } = new();
        public string LastTakenDatesJson { get; set; } = "[]";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User? User { get; set; }
    }
}