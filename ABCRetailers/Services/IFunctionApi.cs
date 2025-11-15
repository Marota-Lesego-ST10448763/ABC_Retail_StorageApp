using ABCRetailers.Models;

namespace ABCRetailers.Services;

public interface IFunctionsApi
{
    // Customer-related operations
    Task<List<Customer>> GetCustomersAsync();
    Task<Customer?> GetCustomerAsync(string id);
    Task<Customer?> GetCustomerByUsernameAsync(string username); // Lookup customer by username
    Task<Customer> CreateCustomerAsync(Customer c);
    Task<Customer> UpdateCustomerAsync(string id, Customer c);
    Task DeleteCustomerAsync(string id);

    // Product-related operations
    Task<List<Product>> GetProductsAsync();
    Task<Product?> GetProductAsync(string id);
    Task<Product> CreateProductAsync(Product p, IFormFile? imageFile);
    Task<Product> UpdateProductAsync(string id, Product p, IFormFile? imageFile);
    Task DeleteProductAsync(string id);

    // Order-related operations
    Task<List<Order>> GetOrdersAsync();
    Task<Order?> GetOrderAsync(string id);
    Task<Order> CreateOrderAsync(string customerId, string productId, int quantity);
    Task UpdateOrderStatusAsync(string id, string newStatus);
    Task DeleteOrderAsync(string id);

    // Get orders filtered by a specific customer
    Task<List<Order>> GetOrdersByCustomerIdAsync(string customerId);

    // File upload operations
    Task<string> UploadProofOfPaymentAsync(IFormFile file, string? orderId, string? customerName);

    // Retrieve all uploaded documents (admin use)
    Task<List<UploadedDocument>> GetUploadedDocumentsAsync();
}
