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

        public string? ExtractedText { get; set; }

        public DateTime? ReportDate { get; set; }

        public string? ReportType { get; set; }

        public string ProcessingStatus { get; set; } = "Pending";      
    }
}