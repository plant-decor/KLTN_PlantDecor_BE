using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;
        private readonly ILogger<CloudinaryService> _logger;

        public CloudinaryService(IConfiguration configuration, ILogger<CloudinaryService> logger)
        {
            _logger = logger;

            var cloudName = configuration["Cloudinary:CloudName"];
            var apiKey = configuration["Cloudinary:ApiKey"];
            var apiSecret = configuration["Cloudinary:ApiSecret"];

            if (string.IsNullOrEmpty(cloudName) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
            {
                throw new Exception("Cloudinary configuration is missing or incomplete");
            }

            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
            _cloudinary.Api.Secure = true;
        }

        public async Task<bool> DeleteFileAsync(string publicId)
        {
            try
            {
                var deleteParams = new DeletionParams(publicId)
                {
                    ResourceType = ResourceType.Image
                };

                var result = await _cloudinary.DestroyAsync(deleteParams);
                return result.Result == "ok";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting file with publicId: {publicId}");
                return false;
            }
        }

        public async Task<bool> DeleteFileByUrlAsync(string fileUrl)
        {
            var publicId = ExtractCloudinaryPublicId(fileUrl);
            if (string.IsNullOrWhiteSpace(publicId))
            {
                _logger.LogWarning("Unable to extract Cloudinary publicId from URL: {FileUrl}", fileUrl);
                return false;
            }

            return await DeleteFileAsync(publicId);
        }

        public async Task<List<bool>> DeleteFilesAsync(List<string> publicIds)
        {
            var results = new List<bool>();

            foreach (var publicId in publicIds)
            {
                var result = await DeleteFileAsync(publicId);
                results.Add(result);
            }

            return results;
        }

        public bool IsValidFileSize(IFormFile file, long maxSizeInBytes)
        {
            return file != null && file.Length > 0 && file.Length <= maxSizeInBytes;
        }

        public bool IsValidFileType(IFormFile file, List<string> allowedExtensions)
        {
            if (file == null || string.IsNullOrEmpty(file.FileName))
                return false;

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            return allowedExtensions.Contains(extension);
        }

        public async Task<FileUploadResponse> UploadFileAsync(IFormFile file, string folder = "tests")
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    throw new BadRequestException("File is null or empty");
                }

                // Get file extension for proper handling
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

                // Create upload parameters for documents
                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(file.FileName, file.OpenReadStream()),
                    Folder = folder,
                    PublicId = $"{folder}/{Guid.NewGuid()}_{Path.GetFileNameWithoutExtension(file.FileName)}",
                    UseFilename = false,
                    UniqueFilename = true,
                    Overwrite = false
                };

                // Log upload attempt
                _logger.LogInformation($"Uploading file: {file.FileName} ({file.Length} bytes) to folder: {folder}");

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                if (uploadResult.Error != null)
                {
                    _logger.LogError($"Cloudinary upload error: {uploadResult.Error.Message}");
                    throw new BadRequestException($"Upload failed: {uploadResult.Error.Message}");
                }

                _logger.LogInformation($"Successfully uploaded file: {file.FileName} -> {uploadResult.SecureUrl}");

                return new FileUploadResponse
                {
                    PublicId = uploadResult.PublicId,
                    Url = uploadResult.Url?.ToString() ?? string.Empty,
                    SecureUrl = uploadResult.SecureUrl?.ToString() ?? string.Empty,
                    OriginalFileName = file.FileName ?? string.Empty,
                    FileType = fileExtension,
                    FileSize = file.Length,
                    UploadedAt = DateTime.UtcNow,
                    Width = uploadResult.Width,
                    Height = uploadResult.Height,
                    Format = uploadResult.Format,
                    ResourceType = uploadResult.ResourceType
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error uploading file {file?.FileName} to Cloudinary");
                throw;
            }
        }

        public async Task<FileUploadResponse> UploadImageBytesAsync(byte[] fileBytes, string fileName, string folder = "tests")
        {
            if (fileBytes == null || fileBytes.Length == 0)
            {
                throw new BadRequestException("Image bytes are null or empty");
            }

            var normalizedFileName = string.IsNullOrWhiteSpace(fileName)
                ? $"generated_{Guid.NewGuid():N}.png"
                : fileName;

            var fileExtension = Path.GetExtension(normalizedFileName).ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(fileExtension))
            {
                fileExtension = ".png";
                normalizedFileName = $"{normalizedFileName}.png";
            }

            await using var stream = new MemoryStream(fileBytes);
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(normalizedFileName, stream),
                Folder = folder,
                PublicId = $"{folder}/{Guid.NewGuid()}_{Path.GetFileNameWithoutExtension(normalizedFileName)}",
                UseFilename = false,
                UniqueFilename = true,
                Overwrite = false
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);
            if (uploadResult.Error != null)
            {
                _logger.LogError("Cloudinary upload error for generated image: {Message}", uploadResult.Error.Message);
                throw new BadRequestException($"Upload failed: {uploadResult.Error.Message}");
            }

            return new FileUploadResponse
            {
                PublicId = uploadResult.PublicId,
                Url = uploadResult.Url?.ToString() ?? string.Empty,
                SecureUrl = uploadResult.SecureUrl?.ToString() ?? string.Empty,
                OriginalFileName = normalizedFileName,
                FileType = fileExtension,
                FileSize = fileBytes.Length,
                UploadedAt = DateTime.UtcNow,
                Width = uploadResult.Width,
                Height = uploadResult.Height,
                Format = uploadResult.Format,
                ResourceType = uploadResult.ResourceType
            };
        }

        public async Task<List<FileUploadResponse>> UploadFilesAsync(List<IFormFile> files, string folder = "tests")
        {
            var results = new List<FileUploadResponse>();
            var errors = new List<string>();

            foreach (var file in files)
            {
                try
                {
                    var result = await UploadFileAsync(file, folder);
                    results.Add(result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to upload file: {file.FileName}");
                    errors.Add($"Failed to upload {file.FileName}: {ex.Message}");
                }
            }

            if (errors.Any() && !results.Any())
            {
                throw new BadRequestException($"All uploads failed: {string.Join("; ", errors)}");
            }

            if (errors.Any())
            {
                _logger.LogWarning($"Some uploads failed: {string.Join("; ", errors)}");
            }

            return results;
        }

        public (bool IsValid, string ErrorMessage) ValidateDocumentFile(IFormFile file, int maxSizeInMB = 10)
        {
            if (file == null)
                return (false, "File is null");

            if (file.Length == 0)
                return (false, "File is empty");

            // Check file extension
            var allowedExtensions = new List<string> { ".jpg", ".jpeg", ".png" };
            if (!IsValidFileType(file, allowedExtensions))
            {
                return (false, $"Invalid file type. Allowed types: {string.Join(", ", allowedExtensions)}");
            }

            // Check file size
            var maxSizeInBytes = maxSizeInMB * 1024 * 1024; // Convert MB to Bytes
            if (!IsValidFileSize(file, maxSizeInBytes))
            {
                var fileSizeInMB = file.Length / (1024.0 * 1024.0); // Convert Bytes to MB
                return (false, $"File size ({fileSizeInMB:F2}MB) exceeds maximum allowed size ({maxSizeInMB}MB)");
            }

            // Check for malicious file names
            var fileName = Path.GetFileName(file.FileName);
            if (string.IsNullOrWhiteSpace(fileName) || fileName.Contains("..") || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                return (false, "Invalid file name");
            }

            return (true, string.Empty);
        }

        private static string ExtractCloudinaryPublicId(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return string.Empty;
            }

            try
            {
                var uri = new Uri(imageUrl);
                var path = uri.AbsolutePath;
                var uploadIndex = path.IndexOf("/upload/", StringComparison.Ordinal);
                if (uploadIndex < 0)
                {
                    return string.Empty;
                }

                var afterUpload = path[(uploadIndex + "/upload/".Length)..];
                if (afterUpload.StartsWith('v') && afterUpload.Contains('/'))
                {
                    afterUpload = afterUpload[(afterUpload.IndexOf('/') + 1)..];
                }

                var extensionIndex = afterUpload.LastIndexOf('.');
                return extensionIndex > 0
                    ? afterUpload[..extensionIndex]
                    : afterUpload;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
