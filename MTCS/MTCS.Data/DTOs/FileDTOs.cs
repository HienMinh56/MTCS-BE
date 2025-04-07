using Microsoft.AspNetCore.Http;

namespace MTCS.Data.DTOs
{
    public class FileUploadDTO
    {
        public IFormFile File { get; set; }
        public string Description { get; set; }
        public string? Note { get; set; }
    }

    public class FileDetailsDTO
    {
        public string? Description { get; set; }
        public string? Note { get; set; }
    }
}
