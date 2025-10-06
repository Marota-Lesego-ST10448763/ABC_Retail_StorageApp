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
    public class StoreCustomerFunction
    {
        private readonly TableServiceClient _tableServiceClient;
        private readonly ILogger _logger;

        public StoreCustomerFunction(IConfiguration config, ILoggerFactory loggerFactory)
        {
            var connectionString = config.GetConnectionString("AzureStorage") ??
                                   Environment.GetEnvironmentVariable("AzureStorage");
            _tableServiceClient = new TableServiceClient(connectionString);
            _logger = loggerFactory.CreateLogger<StoreCustomerFunction>();
        }

        [Function("StoreCustomerFunction")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            try
            {
                var body = await new StreamReader(req.Body).ReadToEndAsync();
                var customer = JsonSerializer.Deserialize<Customer>(body, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (customer == null)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid customer data.");
                    return bad;
                }

                var tableClient = _tableServiceClient.GetTableClient("Customers");
                await tableClient.CreateIfNotExistsAsync();
                await tableClient.AddEntityAsync(customer);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteStringAsync($"Customer {customer.Name} stored successfully.");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing customer data.");
                var error = req.CreateResponse(HttpStatusCode.InternalServerError);
                await error.WriteStringAsync($"Error: {ex.Message}");
                return error;
            }
        }
    }
}
