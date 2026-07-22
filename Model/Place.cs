using System;
using System.ComponentModel.DataAnnotations;

namespace MedVaultAPI.Models
{
    public class Place
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string Name { get; set; } = string.Empty;

        public double Latitude { get; set; }

        public double Longitude { get; set; }
    }
}