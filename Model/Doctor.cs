using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace MedVaultAPI.Models
{
    public class Doctor
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Specialty { get; set; } = string.Empty;

        [Required]
        public string HospitalName { get; set; } = string.Empty;

        [Required]
        public string HospitalType { get; set; } = string.Empty; 

        [Required]
        public string Location { get; set; } = string.Empty;
    }
}