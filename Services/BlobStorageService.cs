#nullable disable
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using FutureTech_StudentManagement.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace FutureTech_StudentManagement.Services
{
    public class BlobStorageService : IBlobStorageService
    {
        private readonly BlobContainerClient _containerClient;

        public BlobStorageService(IConfiguration configuration)
        {
            var connectionString = configuration["Azure:BlobStorage:ConnectionString"];
            var containerName = configuration["Azure:BlobStorage:ContainerName"];

            var blobServiceClient = new BlobServiceClient(connectionString);
            _containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            _containerClient.CreateIfNotExists();
        }

        public async Task<string> UploadFileAsync(IFormFile file, string fileName)
        {
            
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                throw new ArgumentException("Only JPEG and PNG images are allowed.");
            }

            // Validate file size (max 5MB)
            if (file.Length > 5 * 1024 * 1024)
            {
                throw new ArgumentException("File size cannot exceed 5MB.");
            }

            
            using var imageStream = new MemoryStream();
            using (var image = await Image.LoadAsync(file.OpenReadStream()))
            {
                
                if (image.Width > 800 || image.Height > 800)
                {
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Mode = ResizeMode.Max,
                        Size = new Size(800, 800)
                    }));
                }

                
                var encoder = new JpegEncoder { Quality = 80 };
                await image.SaveAsJpegAsync(imageStream, encoder);
                imageStream.Position = 0;
            }

            // Upload to blob storage
            var blobClient = _containerClient.GetBlobClient(fileName);

            var blobHttpHeaders = new BlobHttpHeaders
            {
                ContentType = "image/jpeg"
            };

            await blobClient.UploadAsync(imageStream, new BlobUploadOptions
            {
                HttpHeaders = blobHttpHeaders
            });

            return blobClient.Uri.ToString();
        }

        public async Task<string> GetFileUrlWithSasTokenAsync(string blobName)
        {
            if (string.IsNullOrEmpty(blobName))
                return null;

            var blobClient = _containerClient.GetBlobClient(blobName);

            if (!await blobClient.ExistsAsync())
            {
                return null;
            }

            // Create SAS token valid for 1 hour
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerClient.Name,
                BlobName = blobName,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var sasUri = blobClient.GenerateSasUri(sasBuilder);
            return sasUri.ToString();
        }

        public async Task<bool> DeleteFileAsync(string blobName)
        {
            if (string.IsNullOrEmpty(blobName))
                return false;

            var blobClient = _containerClient.GetBlobClient(blobName);
            return await blobClient.DeleteIfExistsAsync();
        }

        public string ExtractBlobNameFromUrl(string blobUrl)
        {
            if (string.IsNullOrEmpty(blobUrl))
                return null;

            try
            {
                var uri = new Uri(blobUrl);
                return Path.GetFileName(uri.LocalPath);
            }
            catch
            {
                return null;
            }
        }
    }
}
