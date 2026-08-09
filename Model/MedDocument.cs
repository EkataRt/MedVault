namespace MedVaultAPI.Model
{
    public class MedDocument
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string FolderId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;

        // --- NEW FIELDS FOR SMART SEARCH ---
        public string? ExtractedText { get; set; }
        public DateTime? ReportDate { get; set; }
        public string? ReportType { get; set; } // e.g., "MRI", "Blood Test"
        public string ProcessingStatus { get; set; } = "Uploaded"; // Uploaded, Processing, Processed, Failed

        // Navigation property for extracted measurements
        public ICollection<MedicalMeasurement> Measurements { get; set; } = new List<MedicalMeasurement>();
    }
}
