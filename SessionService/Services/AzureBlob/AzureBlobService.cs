namespace SessionService.Services.AzureBlob
{
    using Azure.Storage.Blobs;
    using System.IO;
    using System.Threading.Tasks;

    public class AzureBlobService
    {
        private readonly BlobServiceClient _blobServiceClient;

        public AzureBlobService(string connectionString)
        {
            _blobServiceClient = new BlobServiceClient(connectionString);
        }

        public async Task<Stream> DownloadFileAsync(string containerName, string blobName)
        {
            var blobContainerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = blobContainerClient.GetBlobClient(blobName);
            var response = await blobClient.DownloadAsync();
            return response.Value.Content;
        }
    }
}
