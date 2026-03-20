namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class FileUploadResponse
    {
        public string PublicId { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string SecureUrl { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public long Width { get; set; }
        public long Height { get; set; }
        public string Format { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
        public string? ResourceType { get; set; }
    }
}
