namespace MedVaultAPI.Model
{
    public class Folder
    {
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? ParentId { get; set; }

        public string UserId { get; set; } = string.Empty;

        public string CreatedAt { get; set; } = string.Empty;

    }
}