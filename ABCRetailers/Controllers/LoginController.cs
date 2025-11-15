using ABCRetailers.Data;
using ABCRetailers.Models;
using ABCRetailers.Models.ViewModels;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ABCRetailers.Controllers
{
    public class LoginController : Controller
    {
        private readonly AuthDbContext _db;
        private readonly IFunctionsApi _functionsApi;
        private readonly ILogger<LoginController> _logger;

        public LoginController(AuthDbContext db, IFunctionsApi functionsApi, ILogger<LoginController> logger)
        {
            _db = db;
            _functionsApi = functionsApi;
            _logger = logger;
        }

        // Shows the login page
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Index(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        // Handles login attempts
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                // Step 1: Check if user exists in SQL
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == model.Username);
                if (user == null)
                {
                    ViewBag.Error = "Invalid username or password.";
                    return View(model);
                }

                // Basic password check (hashing can be added later)
                if (user.PasswordHash != model.Password)
                {
                    ViewBag.Error = "Invalid username or password.";
                    return View(model);
                }

                // Step 2: Get customer record from Azure if user is a customer
                string customerId = "";

                if (user.Role == "Customer")
                {
                    var customer = await _functionsApi.GetCustomerByUsernameAsync(user.Username);
                    if (customer == null)
                    {
                        _logger.LogWarning("No customer found in Azure for username {Username}", user.Username);
                        ViewBag.Error = "Customer record not found. Please contact support.";
                        return View(model);
                    }
                    customerId = customer.Id;
                }

                // Step 3: Create claims for authentication
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role)
                };

                if (!string.IsNullOrEmpty(customerId))
                {
                    claims.Add(new Claim("CustomerId", customerId));
                }

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                // Step 4: Sign the user in
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(60)
                    });

                // Step 5: Save session information
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("Role", user.Role);
                if (!string.IsNullOrEmpty(customerId))
                {
                    HttpContext.Session.SetString("CustomerId", customerId);
                }

                _logger.LogInformation("User {Username} logged in as {Role}", user.Username, user.Role);

                // Step 6: Redirect user to the correct dashboard
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return user.Role switch
                {
                    "Admin" => RedirectToAction("AdminDashboard", "Home"),
                    _ => RedirectToAction("CustomerDashboard", "Home")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed for user {Username}", model.Username);
                ViewBag.Error = "Unexpected error during login. Please try again.";
                return View(model);
            }
        }

        // Shows the registration page
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        // Handles registration
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Step 1: Make sure username isn't taken
            var exists = await _db.Users.AnyAsync(u => u.Username == model.Username);
            if (exists)
            {
                ViewBag.Error = "Username already exists.";
                return View(model);
            }

            try
            {
                // Step 2: Save the user locally (SQL)
                var user = new User
                {
                    Username = model.Username,
                    PasswordHash = model.Password,
                    Role = model.Role
                };

                _db.Users.Add(user);
                await _db.SaveChangesAsync();

                _logger.LogInformation("User saved to SQL: {Username} ({Role})", model.Username, model.Role);

                // Step 3: Create Azure customer record if needed
                if (model.Role == "Customer")
                {
                    var customer = new Customer
                    {
                        Username = model.Username,
                        Name = model.FirstName,
                        Surname = model.LastName,
                        Email = model.Email ?? "",
                        ShippingAddress = model.ShippingAddress ?? ""
                    };

                    _logger.LogInformation("Creating Azure customer for {Username}", customer.Username);

                    try
                    {
                        var createdCustomer = await _functionsApi.CreateCustomerAsync(customer);
                        _logger.LogInformation("Azure customer created with ID {Id}", createdCustomer.Id);
                    }
                    catch (Exception azureEx)
                    {
                        _logger.LogError(azureEx, "Failed to create Azure customer for {Username}", model.Username);
                    }
                }
                else
                {
                    _logger.LogInformation("Admin registered — no Azure customer creation required");
                }

                TempData["Success"] = "Registration complete! You can now log in.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed for user {Username}", model.Username);
                ViewBag.Error = $"Could not complete registration: {ex.Message}";
                return View(model);
            }
        }

        // Logs the user out
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // Access denied page
        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied() => View();
    }
}
