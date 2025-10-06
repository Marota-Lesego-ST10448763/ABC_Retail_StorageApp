using System.Text.Json;
using ABC_Retail_StorageApp.Models;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace ABC_Retail_Functions
{
    public class ProcessOrderQueueFunction
    {
        private readonly TableServiceClient _tableServiceClient;
        private readonly ILogger _logger;

        public ProcessOrderQueueFunction(IConfiguration config, ILoggerFactory loggerFactory)
        {
            var connectionString = config.GetConnectionString("AzureStorage") ??
                                   Environment.GetEnvironmentVariable("AzureStorage");
            _tableServiceClient = new TableServiceClient(connectionString);
            _logger = loggerFactory.CreateLogger<ProcessOrderQueueFunction>();
        }

        [Function("ProcessOrderQueueFunction")]
        public async Task Run(
            [QueueTrigger("order-notifications", Connection = "AzureStorage")] string queueMessage)
        {
            try
            {
                _logger.LogInformation($"Received queue message: {queueMessage}");
                var order = JsonSerializer.Deserialize<Order>(queueMessage);
                if (order == null)
                {
                    _logger.LogWarning("Invalid order message received.");
                    return;
                }

                var tableClient = _tableServiceClient.GetTableClient("Orders");
                await tableClient.CreateIfNotExistsAsync();
                await tableClient.AddEntityAsync(order);

                _logger.LogInformation($"Order {order.OrderID} saved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing order queue message.");
                throw;
            }
        }
    }
}
