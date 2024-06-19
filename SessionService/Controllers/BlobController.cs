using Microsoft.AspNetCore.Mvc;
using SessionService.Services.AzureBlob;

namespace SessionService.Controllers
{
    public class BlobController : Controller
    {
        private readonly AzureBlobService _blobService;

        public BlobController(AzureBlobService blobService)
        {
            _blobService = blobService;
        }

        [HttpGet("session/download/{blobName}")]
        public async Task<IActionResult> Download(string blobName)
        {
            var stream = await _blobService.DownloadFileAsync("songs", blobName);
            return File(stream, "audio/mpeg");
        }
    }
}
