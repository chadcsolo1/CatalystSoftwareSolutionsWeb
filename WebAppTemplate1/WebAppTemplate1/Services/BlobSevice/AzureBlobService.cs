using Azure.Storage;
using Azure.Storage.Blobs;
using WebAppTemplate1.Models;

namespace WebAppTemplate1.Services.BlobSevice
{
    public class AzureBlobService
    {
        private readonly string _storageAccount = "";
        private readonly string _key = "";
        private readonly BlobContainerClient _videoContainer;

        public AzureBlobService()
        {
            var credential = new StorageSharedKeyCredential(_storageAccount, _key);
            var blobUrl = $"https://{_storageAccount}.blob.core.windows.net";
            var client = new BlobServiceClient(new Uri(blobUrl), credential);
            _videoContainer = client.GetBlobContainerClient("videos");
        }


        public async Task<List<Video>> GetVideosAsync()
        {
            var videos = new List<Video>();
            var videoBlobs = _videoContainer.GetBlobsAsync();


            await foreach (var blobItem in videoBlobs)
            {
                var blobClient = _videoContainer.GetBlobClient(blobItem.Name);
                var videoUrl = blobClient.Uri.ToString();
                videos.Add(new Video { Title = blobItem.Name, Url = videoUrl });
            }
            return videos;
        }

    }
}
