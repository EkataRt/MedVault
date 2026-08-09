namespace MedVaultAPI.Model
{//OCR
    public class MedicalMeasurement
    {
        public string Id { get; set; } = string.Empty;

        public string UserId { get; set; } = string.Empty;

        public string DocumentId { get; set; } = string.Empty;

        public string MeasurementType { get; set; } = string.Empty;

        public double Value { get; set; }

        public double? SecondaryValue { get; set; }

        public string? Unit { get; set; }

        public DateTime? MeasuredDate { get; set; }
    }
}
