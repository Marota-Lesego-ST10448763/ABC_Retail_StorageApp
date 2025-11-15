using ABCRetailers.Services;
using ABCRetailers.Data;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// -----------------------------------------
// Add MVC and HTTP Context Accessor
// -----------------------------------------
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// -----------------------------------------
// Configure Entity Framework Core with Azure SQL
// -----------------------------------------
builder.Services.AddDbContext<AuthDbContext>(options =>
{
    var connStr = builder.Configuration.GetConnectionString("AuthDatabase");
    options.UseSqlServer(connStr);
});

// -----------------------------------------
// Register Azure Functions API Client
// -----------------------------------------
builder.Services.AddHttpClient("Functions", (sp, client) =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var baseUrl = cfg["FunctionApi:BaseUrl"]
        ?? throw new InvalidOperationException("FunctionApi:BaseUrl missing");

    // Ensure proper API route
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/api/");
    client.Timeout = TimeSpan.FromSeconds(100);
});

builder.Services.AddScoped<IFunctionsApi, FunctionsApiClient>();

// -----------------------------------------
// Configure cookie-based authentication
// -----------------------------------------
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login/Index";
        options.AccessDeniedPath = "/Login/AccessDenied";
        options.Cookie.Name = "ABCAuthCookie";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
    });

// -----------------------------------------
// Set up session management
// -----------------------------------------
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "ABCSession";
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// -----------------------------------------
// Configure file upload limits
// -----------------------------------------
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 50 * 1024 * 1024; // Maximum 50 MB
});

// -----------------------------------------
// Build the app
// -----------------------------------------
var app = builder.Build();

// -----------------------------------------
// Set default culture
// -----------------------------------------
var culture = new CultureInfo("en-US");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

// -----------------------------------------
// Middleware pipeline
// -----------------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Session should be enabled before authentication
app.UseSession();

// Enable authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

// -----------------------------------------
// Define default routes
// -----------------------------------------
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
