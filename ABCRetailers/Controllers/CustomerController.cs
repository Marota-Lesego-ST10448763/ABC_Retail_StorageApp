using ABCRetailers.Models;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetailers.Controllers
{
    // Only admins can access this controller
    [Authorize(Roles = "Admin")] 
    public class CustomerController : Controller
    {
        private readonly IFunctionsApi _api;
        public CustomerController(IFunctionsApi api) => _api = api;

        // Shows a list of all customers
        public async Task<IActionResult> Index()
        {
            var customers = await _api.GetCustomersAsync();
            return View(customers);
        }

        // Displays the create customer form
        public IActionResult Create() => View();

        // Handles creation of a new customer
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer customer)
        {
            if (!ModelState.IsValid) return View(customer);

            try
            {
                await _api.CreateCustomerAsync(customer);
                TempData["Success"] = "Customer created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error creating customer: {ex.Message}");
                return View(customer);
            }
        }

        // Loads an existing customer so it can be edited
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var customer = await _api.GetCustomerAsync(id);
            return customer is null ? NotFound() : View(customer);
        }

        // Handles saving changes to a customer
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Customer customer)
        {
            if (!ModelState.IsValid) return View(customer);

            try
            {
                await _api.UpdateCustomerAsync(customer.Id, customer);
                TempData["Success"] = "Customer updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error updating customer: {ex.Message}");
                return View(customer);
            }
        }

        // Deletes a customer
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _api.DeleteCustomerAsync(id);
                TempData["Success"] = "Customer deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting customer: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
