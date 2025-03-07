using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCS.Service
{
    public interface IFirebaseStorageService
    {
        Task<string> UploadImageAsync(IFormFile imageFile, string? imageName = default);

        string GetImageUrl(string imageName);

        Task<string> UpdateImageAsync(IFormFile imageFile, string imageName);

        Task DeleteImageAsync(string imageName);

        Task<string[]> UploadImagesAsync(IFormFileCollection files);
        Task<FileMetadata> UploadFileAsync(IFormFile file, string? fileName = default);
        string ExtractImageNameFromUrl(string imageUrl);
    }

    public class FileMetadata
    {
        public string FileUrl { get; set; }
        public string FileName { get; set; }
        public string FileType { get; set; }
        public long FileSize { get; set; }
    }
    public class FirebaseStorageService : IFirebaseStorageService
    {
        private readonly StorageClient _storageClient;
        private readonly IConfiguration _configuration;
        private readonly string _bucketName;

        public FirebaseStorageService(IConfiguration configuration, ILogger<FirebaseStorageService> logger)
        {
            _configuration = configuration;
            _bucketName = _configuration["Firebase:Bucket"]!;

            try
            {
                var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                logger.LogInformation($"Current Environment: {environment}");

                GoogleCredential googleCredential;

                if (environment == "Production")
                {
                    var base64JsonAuth = Environment.GetEnvironmentVariable("FIREBASE_CREDENTIALS");

                    if (string.IsNullOrEmpty(base64JsonAuth))
                    {
                        throw new InvalidOperationException("🔥 FIREBASE_CREDENTIALS environment variable is missing.");
                    }

                    var jsonAuthBytes = Convert.FromBase64String(base64JsonAuth);
                    var jsonAuth = Encoding.UTF8.GetString(jsonAuthBytes);
                    googleCredential = GoogleCredential.FromJson(jsonAuth);

                }
                else
                {
                    var firebaseAuthPath = _configuration["Firebase:AuthFile"];

                    if (!File.Exists(firebaseAuthPath))
                    {
                        throw new FileNotFoundException($"🔥 Firebase Auth file not found: {firebaseAuthPath}");
                    }

                    googleCredential = GoogleCredential.FromFile(firebaseAuthPath);
                }

                _storageClient = StorageClient.Create(googleCredential);
            }
            catch (Exception ex)
            {
                throw;
            }
        }


        public async Task DeleteImageAsync(string imageName)
        {
            await _storageClient.DeleteObjectAsync(_bucketName, imageName, cancellationToken: CancellationToken.None);
        }

        public string GetImageUrl(string imageName)
        {

            string imageUrl = $"https://firebasestorage.googleapis.com/v0/b/{_bucketName}/o/{Uri.EscapeDataString(imageName)}?alt=media";
            return imageUrl;
        }

        public async Task<string> UpdateImageAsync(IFormFile imageFile, string imageName)
        {

            using var stream = new MemoryStream();

            await imageFile.CopyToAsync(stream);

            stream.Position = 0;

            // Re-upload the image with the same name to update it
            var blob = await _storageClient.UploadObjectAsync(_bucketName, imageName, imageFile.ContentType, stream, cancellationToken: CancellationToken.None);

            return GetImageUrl(imageName);
        }


        public async Task<string> UploadImageAsync(IFormFile imageFile, string? imageName = default)
        {
            imageName ??= $"{Path.GetFileNameWithoutExtension(imageFile.FileName)}_{Guid.NewGuid()}{Path.GetExtension(imageFile.FileName)}";

            //            imageName += $"_{Guid.NewGuid()}";
            using var stream = new MemoryStream();

            await imageFile.CopyToAsync(stream);

            var blob = await _storageClient.UploadObjectAsync(_bucketName, imageName, imageFile.ContentType, stream, cancellationToken: CancellationToken.None);

            if (blob is null)
            {
                throw new Exception("Upload image failed");
            }

            return GetImageUrl(imageName);

        }

        public async Task<FileMetadata> UploadFileAsync(IFormFile file, string? fileName = default)
        {
            fileName ??= $"{Path.GetFileNameWithoutExtension(file.FileName)}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            // Create upload options with metadata
            var options = new UploadObjectOptions
            {
                PredefinedAcl = PredefinedObjectAcl.PublicRead
            };

            // Upload the file
            var blob = await _storageClient.UploadObjectAsync(
                _bucketName,
                fileName,
                file.ContentType,
                stream,
                options: options,
                cancellationToken: CancellationToken.None);

            if (blob is null)
            {
                throw new Exception("Upload file failed");
            }

            // Get metadata from the uploaded object
            var objectMetadata = await _storageClient.GetObjectAsync(_bucketName, fileName);

            // Return file metadata
            return new FileMetadata
            {
                FileUrl = GetImageUrl(fileName),
                FileName = fileName,
                FileType = file.ContentType,
                FileSize = (long?)objectMetadata.Size ?? file.Length
            };
        }

        public async Task<string[]> UploadImagesAsync(IFormFileCollection files)
        {
            var uploadTasks = new List<Task<string>>();

            foreach (var file in files)
            {
                uploadTasks.Add(UploadImageAsync(file));
            }

            var imageUrls = await Task.WhenAll(uploadTasks);

            return imageUrls;
        }
        public string ExtractImageNameFromUrl(string imageUrl)
        {
            // Find the position of 'o/' in the URL
            int start = imageUrl.IndexOf("o/") + 2;  // +2 to skip past 'o/'

            // Find the position of '?alt=' which marks the end of the image name
            int end = imageUrl.IndexOf("?alt=media");

            // Extract the image name from the URL
            string imageName = imageUrl.Substring(start, end - start);

            return imageName;
        }
    }
}