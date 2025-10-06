using System.Net;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace ABC_Retail_Functions
{
    public class UploadProductImageFunction
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly ILogger _logger;

        public UploadProductImageFunction(IConfiguration config, ILoggerFactory loggerFactory)
        {
            var connectionString = config.GetConnectionString("AzureStorage") ??
                                   Environment.GetEnvironmentVariable("AzureStorage");
            _blobServiceClient = new BlobServiceClient(connectionString);
            _logger = loggerFactory.CreateLogger<UploadProductImageFunction>();
        }

        [Function("UploadProductImageFunction")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            try
            {
                var form = await req.ReadFormAsync();
                var file = form.Files["file"];
                if (file == null)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("No file found in request.");
                    return bad;
                }

                var containerClient = _blobServiceClient.GetBlobContainerClient("product-images");
                await containerClient.CreateIfNotExistsAsync(Azure.Storage.Blobs.Models.PublicAccessType.Blob);

                var blobName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var blobClient = containerClient.GetBlobClient(blobName);
                using var stream = file.OpenReadStream();
                await blobClient.UploadAsync(stream, overwrite: true);

                var ok = req.CreateResponse(HttpStatusCode.OK);
                await ok.WriteStringAsync($"Image uploaded: {blobClient.Uri}");
                return ok;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image.");
                var error = req.CreateResponse(HttpStatusCode.InternalServerError);
                await error.WriteStringAsync($"Error: {ex.Message}");
                return error;
            }
        }
    }
}
