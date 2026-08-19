using System.ComponentModel.DataAnnotations.Schema;

namespace MedVaultAPI.Models
{
    public class HealthProfile
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }

        [NotMapped]
        public int Age => ComputeAge(DateOfBirth);

        public string Sex { get; set; } = string.Empty;
        public string BloodType { get; set; } = string.Empty;
        public float Height { get; set; }
        public float Weight { get; set; }
        public float Bmi { get; set; }
        public string LastCheckup { get; set; } = string.Empty;

        [NotMapped]
        public List<string> Allergies { get; set; } = new();
        public string AllergiesJson { get; set; } = "[]";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public User? User { get; set; }

        private static int ComputeAge(DateTime dateOfBirth)
        {
            var today = DateTime.UtcNow.Date;
            var age = today.Year - dateOfBirth.Year;
            if (dateOfBirth.Date > today.AddYears(-age))
            {
                age--;
            }
            return age;
        }
    }
}