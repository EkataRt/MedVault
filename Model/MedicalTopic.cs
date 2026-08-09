namespace MedVaultAPI.Model
{//OCR
    public class MedicalTopic
    {
        public string Id { get; set; } = string.Empty;

        public string UserId { get; set; } = string.Empty;

        public string DocumentId { get; set; } = string.Empty;

        public string Topic { get; set; } = string.Empty;

        public double ConfidenceScore { get; set; }
    }
}
