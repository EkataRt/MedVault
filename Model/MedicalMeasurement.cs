namespace MedVaultAPI.Model
{
    public class MedicalMeasurement
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string DocumentId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;

        public string MeasurementType { get; set; } = string.Empty; // Canonical name, e.g. "CREATININE"
        public double Value { get; set; }
        public double? SecondaryValue { get; set; } // For Systolic/Diastolic like 135/85
        public string Unit { get; set; } = string.Empty; // e.g., "mg/dL"
        public DateTime MeasuredDate { get; set; }

        public MedDocument? Document { get; set; }
    }
}
