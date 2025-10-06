using System.Net;
using System.Text.Json;
using ABC_Retail_StorageApp.Models;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace ABC_Retail_Functions
{
    public class OrderStatusUpdateFunction
    {
        private readonly TableServiceClient _tableServiceClient;
        private readonly ILogger _logger;

        public OrderStatusUpdateFunction(IConfiguration config, ILoggerFactory loggerFactory)
        {
            var conn = config.GetConnectionString("AzureStorage") ??
                       Environment.GetEnvironmentVariable("AzureStorage");
            _tableServiceClient = new TableServiceClient(conn);
            _logger = loggerFactory.CreateLogger<OrderStatusUpdateFunction>();
        }

        [Function("OrderStatusUpdateFunction")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put")] HttpRequestData req)
        {
            try
            {
                var body = await new StreamReader(req.Body).ReadToEndAsync();
                var updateRequest = JsonSerializer.Deserialize<Order>(body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (updateRequest == null)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid order data.");
                    return bad;
                }

                var table = _tableServiceClient.GetTableClient("Orders");
                var existing = await table.GetEntityAsync<Order>("Order", updateRequest.RowKey);
                var order = existing.Value;

                order.Status = updateRequest.Status;
                await table.UpdateEntityAsync(order, order.ETag, TableUpdateMode.Replace);

                var ok = req.CreateResponse(HttpStatusCode.OK);
                await ok.WriteStringAsync($"Order {order.OrderID} updated to {order.Status}");
                return ok;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status");
                var err = req.CreateResponse(HttpStatusCode.InternalServerError);
                await err.WriteStringAsync(ex.Message);
                return err;
            }
        }
    }
}
