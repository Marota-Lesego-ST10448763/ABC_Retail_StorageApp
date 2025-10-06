using System.Net;
using Azure.Storage.Files.Shares;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace ABC_Retail_Functions
{
    public class UploadContractFunction
    {
        private readonly ShareServiceClient _shareServiceClient;
        private readonly ILogger _logger;

        public UploadContractFunction(IConfiguration config, ILoggerFactory loggerFactory)
        {
            var connectionString = config.GetConnectionString("AzureStorage") ??
                                   Environment.GetEnvironmentVariable("AzureStorage");
            _shareServiceClient = new ShareServiceClient(connectionString);
            _logger = loggerFactory.CreateLogger<UploadContractFunction>();
        }

        [Function("UploadContractFunction")]
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

                var shareClient = _shareServiceClient.GetShareClient("contracts");
                await shareClient.CreateIfNotExistsAsync();

                var directoryClient = shareClient.GetRootDirectoryClient();
                var fileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{file.FileName}";
                var fileClient = directoryClient.GetFileClient(fileName);

                using var stream = file.OpenReadStream();
                await fileClient.CreateAsync(stream.Length);
                await fileClient.UploadAsync(stream);

                var ok = req.CreateResponse(HttpStatusCode.OK);
                await ok.WriteStringAsync($"File uploaded to contracts share: {fileName}");
                return ok;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading contract.");
                var error = req.CreateResponse(HttpStatusCode.InternalServerError);
                await error.WriteStringAsync($"Error: {ex.Message}");
                return error;
            }
        }
    }
}
