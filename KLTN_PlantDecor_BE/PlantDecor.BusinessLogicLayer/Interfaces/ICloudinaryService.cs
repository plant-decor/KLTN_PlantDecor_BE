using Microsoft.AspNetCore.Http;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface ICloudinaryService
    {
        Task<FileUploadResponse> UploadFileAsync(IFormFile file, string folder = "tests");
        Task<FileUploadResponse> UploadImageBytesAsync(byte[] fileBytes, string fileName, string folder = "tests");
        Task<List<FileUploadResponse>> UploadFilesAsync(List<IFormFile> files, string folder = "tests");
        Task<bool> DeleteFileAsync(string publicId);
        Task<bool> DeleteFileByUrlAsync(string fileUrl);
        Task<List<bool>> DeleteFilesAsync(List<string> publicIds);
        bool IsValidFileType(IFormFile file, List<string> allowedExtensions);
        bool IsValidFileSize(IFormFile file, long maxSizeInBytes);
        (bool IsValid, string ErrorMessage) ValidateDocumentFile(IFormFile file, int maxSizeInMB = 10);

    }
}
