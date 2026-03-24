using Microsoft.AspNetCore.Http;

namespace FutureTech_StudentManagement.Services
{
    public interface IBlobStorageService
    {
        Task<string> UploadFileAsync(IFormFile file, string fileName);
        Task<string> GetFileUrlWithSasTokenAsync(string blobName);
        Task<bool> DeleteFileAsync(string blobName);
        string ExtractBlobNameFromUrl(string blobUrl);
    }
}