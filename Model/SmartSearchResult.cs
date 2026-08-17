namespace MedVaultAPI.Model
{
    public class SmartSearchResult
    {
        public string DocumentId { get; set; } = string.Empty;

        public string DocumentName { get; set; } = string.Empty;

        public string FileName { get; set; } = string.Empty;

        public string? FolderId { get; set; }

        public string? FolderName { get; set; }

        public string? ReportType { get; set; }

        public DateTime? ReportDate { get; set; }

        public string? UploadDate { get; set; }

        public List<string> Topics { get; set; } = new();

        public List<MedicalMeasurement> Measurements { get; set; } = new();

        public double Score { get; set; }
        public double Confidence { get; set; }
    }
}
