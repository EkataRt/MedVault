namespace MedVaultAPI.Models   
{
    public class MedicineNotification
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; 
        public string ReferenceId { get; set; } = string.Empty;
        public int? DoseIndex { get; set; }
        public bool Read { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
    }
}