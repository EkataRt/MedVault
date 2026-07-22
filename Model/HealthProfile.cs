using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace MedVaultAPI.Models
{
    public class HealthProfile
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Sex { get; set; } = string.Empty; // 'Male' | 'Female' | 'Other'
        public string BloodType { get; set; } = string.Empty;
        public float Height { get; set; }
        public float Weight { get; set; }
        public string LastCheckup { get; set; } = string.Empty;

        // Angular sends as string[] — stored as JSON in DB
        [NotMapped]
        public List<string> Allergies { get; set; } = new();
        public string AllergiesJson { get; set; } = "[]";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User? User { get; set; }
    }
}