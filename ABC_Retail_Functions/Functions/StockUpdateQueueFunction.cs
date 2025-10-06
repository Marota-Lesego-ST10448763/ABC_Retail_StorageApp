using System.Text.Json;
using ABC_Retail_StorageApp.Models;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace ABC_Retail_Functions
{
    public class StockUpdateQueueFunction
    {
        private readonly TableServiceClient _tableServiceClient;
        private readonly ILogger _logger;

        public StockUpdateQueueFunction(IConfiguration config, ILoggerFactory loggerFactory)
        {
            var conn = config.GetConnectionString("AzureStorage") ??
                       Environment.GetEnvironmentVariable("AzureStorage");
            _tableServiceClient = new TableServiceClient(conn);
            _logger = loggerFactory.CreateLogger<StockUpdateQueueFunction>();
        }

        [Function("StockUpdateQueueFunction")]
        public async Task Run(
            [QueueTrigger("stock-updates", Connection = "AzureStorage")] string message)
        {
            try
            {
                var update = JsonSerializer.Deserialize<Product>(message);
                if (update == null)
                {
                    _logger.LogWarning("Invalid stock message");
                    return;
                }

                var table = _tableServiceClient.GetTableClient("Products");
                var existing = await table.GetEntityAsync<Product>("Product", update.RowKey);
                var product = existing.Value;

                product.StockAvailable = update.StockAvailable;
                await table.UpdateEntityAsync(product, product.ETag, TableUpdateMode.Replace);

                _logger.LogInformation($"Stock for {product.ProductName} updated to {product.StockAvailable}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing stock update message");
                throw;
            }
        }
    }
}
