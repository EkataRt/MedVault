using System.ComponentModel.DataAnnotations.Schema;

namespace MedVaultAPI.Models
{
    public class BmiHistory
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string ProfileId { get; set; } = string.Empty;

        public float Height { get; set; }
        public float Weight { get; set; }
        public float Bmi { get; set; }

        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    }
}