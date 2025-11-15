using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ABCRetailers.Data;
using ABCRetailers.Models;
using ABCRetailers.Models.ViewModels;
using ABCRetailers.Services;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace ABCRetailers.Controllers
{
    [Authorize(Roles = "Customer")] // Allows only logged-in customers to access this controller
    public class CartController : Controller
    {
        private readonly AuthDbContext _db;
        private readonly IFunctionsApi _api;

        public CartController(AuthDbContext db, IFunctionsApi api)
        {
            _db = db;
            _api = api;
        }

        // Shows the items currently in the user's cart
        public async Task<IActionResult> Index()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Index", "Login");

            var cartItems = await _db.Cart
                .Where(c => c.CustomerUsername == username)
                .ToListAsync();

            var viewModelList = new List<CartItemViewModel>();

            // Load product details for each cart item
            foreach (var item in cartItems)
            {
                var product = await _api.GetProductAsync(item.ProductId);
                if (product == null) continue;

                viewModelList.Add(new CartItemViewModel
                {
                    ProductId = product.Id,
                    ProductName = product.ProductName,
                    Quantity = item.Quantity,
                    UnitPrice = product.Price
                });
            }

            // Return list of cart items to the view
            return View(new CartPageViewModel { Items = viewModelList });
        }

        // Adds a product to the user's cart
        public async Task<IActionResult> Add(string productId)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(productId))
                return RedirectToAction("Index", "Product");

            var product = await _api.GetProductAsync(productId);
            if (product == null)
                return NotFound();

            var existing = await _db.Cart.FirstOrDefaultAsync(c =>
                c.ProductId == productId && c.CustomerUsername == username);

            // If the product is already in the cart, increase the quantity
            if (existing != null)
            {
                existing.Quantity += 1;
            }
            else
            {
                _db.Cart.Add(new Cart
                {
                    CustomerUsername = username,
                    ProductId = productId,
                    Quantity = 1
                });
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = $"{product.ProductName} added to cart.";
            return RedirectToAction("Index", "Product");
        }

        // Handles the checkout process
        [HttpPost]
        public async Task<IActionResult> Checkout()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Index", "Login");

            // Get customer details from Azure Table Storage
            var customer = await _api.GetCustomerByUsernameAsync(username);
            if (customer == null)
            {
                TempData["Error"] = "Customer not found.";
                return RedirectToAction("Index");
            }

            // Load all items in the customer's local cart
            var cartItems = await _db.Cart
                .Where(c => c.CustomerUsername == username)
                .ToListAsync();

            if (!cartItems.Any())
            {
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction("Index");
            }

            // Create orders in Azure for each cart item
            foreach (var item in cartItems)
            {
                await _api.CreateOrderAsync(customer.Id, item.ProductId, item.Quantity);
            }

            // Remove the items from the local cart after checkout
            _db.Cart.RemoveRange(cartItems);
            await _db.SaveChangesAsync();

            // Confirm the order was completed
            TempData["SuccessMessage"] = " Order placed successfully!";
            return RedirectToAction("Confirmation");
        }

        // Shows a simple confirmation message after checkout
        public IActionResult Confirmation()
        {
            ViewBag.Message = TempData["SuccessMessage"] ?? "Thank you for your purchase!";
            return View();
        }

        // Removes a specific product from the cart
        [HttpPost]
        public async Task<IActionResult> Remove(string productId)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return RedirectToAction("Index");

            var item = await _db.Cart.FirstOrDefaultAsync(c =>
                c.CustomerUsername == username && c.ProductId == productId);

            if (item != null)
            {
                _db.Cart.Remove(item);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Item removed from cart.";
            }

            return RedirectToAction("Index");
        }

        // Updates the quantity of items in the cart
        [HttpPost]
        public async Task<IActionResult> UpdateQuantities(List<CartItemViewModel> items)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return RedirectToAction("Index");

            foreach (var item in items)
            {
                var cartItem = await _db.Cart.FirstOrDefaultAsync(c =>
                    c.CustomerUsername == username && c.ProductId == item.ProductId);

                if (cartItem != null)
                {
                    cartItem.Quantity = item.Quantity;
                }
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "Cart updated successfully.";
            return RedirectToAction("Index");
        }
    }
}
